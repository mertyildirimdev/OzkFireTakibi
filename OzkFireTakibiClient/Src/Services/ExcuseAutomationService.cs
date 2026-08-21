using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OzkFireTakibiClient.Src.Data;
using OzkFireTakibiClient.Src.Data.Entities;
using OzkFireTakibiClient.Src.Options;

namespace OzkFireTakibiClient.Src.Services;

/// <summary>
/// Aktif aylık rapordaki tüm kategoriler için mağaza × kategori mazeretlerini transaction içinde üretir.
/// </summary>
public sealed class ExcuseAutomationService(IOptions<ExcuseOptions> options)
{
    private readonly ExcuseOptions _options = options.Value;

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

        var olderRequests = await dbContext.ExcuseRequests
            .Where(request =>
                request.ReportImportId != monthlyImport.Id &&
                request.ReportImport.ReportPeriodId == monthlyImport.ReportPeriodId &&
                request.Status != ExcuseStatus.Superseded)
            .ToArrayAsync(cancellationToken);

        foreach (var request in olderRequests)
        {
            request.StatusBeforeSuperseded = request.Status;
            request.Status = ExcuseStatus.Superseded;
            request.SupersededByReportImportId = monthlyImport.Id;
            request.UpdatedAt = now;
        }

        var existingKeys = await dbContext.ExcuseRequests
            .Where(request => request.ReportImportId == monthlyImport.Id)
            .Select(request => request.StoreNumber + "|" + request.CategoryCode)
            .ToHashSetAsync(cancellationToken);

        var categoryRows = await dbContext.ReportRows
            .AsNoTracking()
            .Where(row =>
                row.ReportImportId == monthlyImport.Id &&
                row.RowType == ReportRowType.CategorySummary &&
                row.CategoryCode != null &&
                row.WasteRate != null)
            .ToArrayAsync(cancellationToken);

        var benchmarks = categoryRows.ToDictionary(
            row => row.CategoryCode!.Trim(),
            row => row,
            StringComparer.OrdinalIgnoreCase);

        var eligibleStores = await dbContext.Stores
            .AsNoTracking()
            .Where(store => store.IsExcuseEligible)
            .Select(store => store.Id)
            .ToHashSetAsync(cancellationToken);

        var storeCategoryRows = await dbContext.ReportRows
            .AsNoTracking()
            .Where(row =>
                row.ReportImportId == monthlyImport.Id &&
                row.RowType == ReportRowType.StoreCategory &&
                row.StoreNumber != null &&
                row.CategoryCode != null &&
                row.WasteRate < 0m)
            .ToArrayAsync(cancellationToken);

        var generated = new List<ExcuseRequestEntity>();

        foreach (var row in storeCategoryRows)
        {
            var storeNumber = row.StoreNumber!.Value;
            var categoryCode = row.CategoryCode!.Trim();
            if (!eligibleStores.Contains(storeNumber) ||
                !benchmarks.TryGetValue(categoryCode, out var categoryRow) ||
                categoryRow.WasteRate is not { } categoryRate)
            {
                continue;
            }

            var categoryMagnitude = Math.Abs(categoryRate);
            var thresholdRate = -(categoryMagnitude * _options.ThresholdMultiplier);
            if (row.WasteRate is not { } storeRate || storeRate > thresholdRate)
            {
                continue;
            }

            var naturalKey = $"{storeNumber}|{categoryCode}";
            if (existingKeys.Contains(naturalKey))
            {
                continue;
            }

            generated.Add(new ExcuseRequestEntity
            {
                ReportImportId = monthlyImport.Id,
                ReportRowId = row.Id,
                StoreNumber = storeNumber,
                StoreName = row.StoreName ?? storeNumber.ToString(),
                CategoryCode = categoryCode,
                CategoryName = row.CategoryName ?? categoryRow.CategoryName ?? categoryCode,
                CategoryAverageWasteRate = categoryRate,
                StoreWasteRate = storeRate,
                ThresholdWasteRate = thresholdRate,
                DeviationPercent = categoryMagnitude == 0m
                    ? 100m
                    : ((Math.Abs(storeRate) / categoryMagnitude) - 1m) * 100m,
                Status = ExcuseStatus.Open,
                CreatedAt = now,
                UpdatedAt = now
            });
            existingKeys.Add(naturalKey);
        }

        await dbContext.ExcuseRequests.AddRangeAsync(generated, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return generated.Count;
    }

    public async Task RestoreSupersededAsync(
        AppDbContext dbContext,
        long restoredReportImportId,
        long deletedReportImportId,
        CancellationToken cancellationToken = default)
    {
        var requests = await dbContext.ExcuseRequests
            .Where(request =>
                request.ReportImportId == restoredReportImportId &&
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
