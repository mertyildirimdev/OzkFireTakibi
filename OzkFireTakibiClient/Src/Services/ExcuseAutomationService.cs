using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OzkFireTakibiClient.Src.Data;
using OzkFireTakibiClient.Src.Data.Entities;
using OzkFireTakibiClient.Src.Options;
using OzkFireTakibiClient.Src.ReportImports;

namespace OzkFireTakibiClient.Src.Services;

/// <summary>
/// Aylık rapor genelini mağaza özetleriyle karşılaştırarak mağaza başına tek otomatik mazeret üretir.
/// </summary>
public sealed class ExcuseAutomationService(IOptions<ExcuseOptions> options)
{
    private readonly ExcuseOptions _options = options.Value;

    /// <summary>
    /// Yeni aktif aylık rapordaki mağaza firelerini rapor geneliyle karşılaştırır ve eşiği aşan,
    /// kapsama dahil mağazalar için yinelenmeyen otomatik talepler oluşturur.
    /// </summary>
    public async Task<int> GenerateAsync(
        AppDbContext dbContext,
        ReportImportEntity monthlyImport,
        CancellationToken cancellationToken = default)
    {
        if (monthlyImport.PeriodType != ReportPeriodType.Monthly)
        {
            return 0;
        }

        if (_options.ThresholdMultiplier <= 1m)
        {
            throw new InvalidOperationException("Mazeret eşik çarpanı 1'den büyük olmalıdır.");
        }

        var now = DateTime.UtcNow;
        await SyncStoresAsync(dbContext, monthlyImport.Id, now, cancellationToken);

        // Aynı dönemin yeni sürümü iş kuralı açısından eski taleplerin yerini alır; geçmiş silinmeden korunur.
        var olderRequests = await dbContext.ExcuseRequests
            .Where(request =>
                request.ReportRow.ReportImportId != monthlyImport.Id &&
                request.ReportRow.ReportImport.ReportPeriodId == monthlyImport.ReportPeriodId &&
                request.Status != ExcuseStatus.Superseded)
            .ToArrayAsync(cancellationToken);

        foreach (var request in olderRequests)
        {
            request.StatusBeforeSuperseded = request.Status;
            request.Status = ExcuseStatus.Superseded;
            request.SupersededByReportImportId = monthlyImport.Id;
            request.UpdatedAt = now;
        }

        var existingRowIds = await dbContext.ExcuseRequests
            .Where(request => request.ReportRow.ReportImportId == monthlyImport.Id)
            .Select(request => request.ReportRowId)
            .ToHashSetAsync(cancellationToken);

        var generalRow = await dbContext.ReportRows
            .AsNoTracking()
            .SingleOrDefaultAsync(row =>
                row.ReportImportId == monthlyImport.Id &&
                row.RowType == ReportRowType.General,
                cancellationToken);

        if (generalRow?.WasteRate is not { } reportRate)
        {
            return 0;
        }

        var eligibleStores = await dbContext.Stores
            .AsNoTracking()
            .Where(store => store.IsExcuseEligible)
            .Select(store => store.Id)
            .ToHashSetAsync(cancellationToken);

        var storeRows = await dbContext.ReportRows
            .AsNoTracking()
            .Where(row =>
                row.ReportImportId == monthlyImport.Id &&
                row.RowType == ReportRowType.StoreSummary &&
                row.StoreNumber != null &&
                row.WasteRate < 0m)
            .ToArrayAsync(cancellationToken);

        var generated = new List<ExcuseRequestEntity>();
        // Fire oranları negatif tutulur. Eşik, genel fire büyüklüğüne çarpan uygulanıp yeniden negatife çevrilir.
        var reportMagnitude = Math.Abs(reportRate);
        var thresholdRate = -(reportMagnitude * _options.ThresholdMultiplier);

        foreach (var row in storeRows)
        {
            var storeNumber = row.StoreNumber!.Value;
            if (!eligibleStores.Contains(storeNumber) ||
                existingRowIds.Contains(row.Id))
            {
                continue;
            }

            if (row.WasteRate is not { } storeRate || storeRate > thresholdRate)
            {
                continue;
            }

            generated.Add(new ExcuseRequestEntity
            {
                ReportRowId = row.Id,
                Source = ExcuseSource.Automatic,
                Title = row.StoreName ?? storeNumber.ToString(),
                ThresholdRate = thresholdRate,
                Status = ExcuseStatus.Open,
                CreatedAt = now,
                UpdatedAt = now
            });
            existingRowIds.Add(row.Id);
        }

        await dbContext.ExcuseRequests.AddRangeAsync(generated, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return generated.Count;
    }

    /// <summary>
    /// Aktif sürüm silinip önceki rapor yeniden etkinleştirildiğinde yalnızca silinen sürümün
    /// geçersiz kıldığı talepleri önceki durumlarına döndürür.
    /// </summary>
    public async Task RestoreSupersededAsync(
        AppDbContext dbContext,
        long restoredReportImportId,
        long deletedReportImportId,
        CancellationToken cancellationToken = default)
    {
        var requests = await dbContext.ExcuseRequests
            .Where(request =>
                request.ReportRow.ReportImportId == restoredReportImportId &&
                request.Status == ExcuseStatus.Superseded &&
                request.SupersededByReportImportId == deletedReportImportId)
            .ToArrayAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var request in requests)
        {
            request.Status = request.StatusBeforeSuperseded ?? ExcuseStatus.Open;
            request.StatusBeforeSuperseded = null;
            request.SupersededByReportImportId = null;
            request.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Rapor mağazalarını merkezi mağaza tablosuyla eşitler; ilk kez görülen depolara yapılandırmadaki
    /// başlangıç kapsam kuralını uygular, mevcut yöneticinin kapsam tercihini değiştirmez.
    /// </summary>
    private async Task SyncStoresAsync(
        AppDbContext dbContext,
        long reportImportId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var reportedStores = await dbContext.ReportRows
            .AsNoTracking()
            .Where(row =>
                row.ReportImportId == reportImportId &&
                row.RowType == ReportRowType.StoreSummary &&
                row.StoreNumber != null)
            .Select(row => new { StoreNumber = row.StoreNumber!.Value, row.StoreName })
            .ToArrayAsync(cancellationToken);

        var storeNumbers = reportedStores.Select(item => item.StoreNumber).ToArray();
        var existingStores = await dbContext.Stores
            .Where(store => storeNumbers.Contains(store.Id))
            .ToDictionaryAsync(store => store.Id, cancellationToken);
        var initiallyExcluded = _options.InitiallyExcludedStoreNumbers.ToHashSet();

        foreach (var reportedStore in reportedStores)
        {
            var name = reportedStore.StoreName ?? reportedStore.StoreNumber.ToString();
            if (existingStores.TryGetValue(reportedStore.StoreNumber, out var existingStore))
            {
                existingStore.Name = name;
                existingStore.UpdatedAt = now;
                continue;
            }

            var store = new StoreEntity
            {
                Id = reportedStore.StoreNumber,
                Name = name,
                IsExcuseEligible = !initiallyExcluded.Contains(reportedStore.StoreNumber),
                CreatedAt = now,
                UpdatedAt = now
            };
            dbContext.Stores.Add(store);
            existingStores.Add(store.Id, store);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
