using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OzkFireTakibi.Dashboard.Data;
using OzkFireTakibi.Dashboard.Models;

namespace OzkFireTakibi.Dashboard.Services;

public sealed class ReportDataService(IDbContextFactory<ReportDbContext> dbContextFactory, IMemoryCache memoryCache)
{
    public async Task<IReadOnlyList<ReportPeriodOption>> GetPeriodsAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var periods = await dbContext.ReportPeriods.AsNoTracking().OrderByDescending(x => x.EndDate).ToArrayAsync(cancellationToken);
        var periodIds = periods.Select(x => x.Id).ToArray();
        var imports = await dbContext.ReportImports.AsNoTracking()
            .Where(x => periodIds.Contains(x.ReportPeriodId) && x.IsActive)
            .OrderByDescending(x => x.Id)
            .ToArrayAsync(cancellationToken);

        return periods.Select(period =>
            {
                var matches = imports.Where(x => x.ReportPeriodId == period.Id).ToArray();
                return new ReportPeriodOption(
                    period.Id,
                    period.EndDate,
                    ToOption(matches.FirstOrDefault(x => x.PeriodType == ReportPeriodType.Monthly)),
                    ToOption(matches.FirstOrDefault(x => x.PeriodType == ReportPeriodType.Cumulative)));
            })
            .Where(x => x.Monthly is not null || x.Cumulative is not null)
            .ToArray();
    }

    public async Task<ReportSnapshot> GetSnapshotAsync(long importId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"report-snapshot:{importId}";
        if (memoryCache.TryGetValue(cacheKey, out ReportSnapshot? cached) && cached is not null)
        {
            return cached;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var importItem = await dbContext.ReportImports.AsNoTracking().SingleAsync(x => x.Id == importId, cancellationToken);
        var includedTypes = new[] { ReportRowType.General, ReportRowType.CategorySummary, ReportRowType.ProductSummary, ReportRowType.StoreProduct };
        var rows = await dbContext.ReportRows.AsNoTracking()
            .Where(x => x.ReportImportId == importId && includedTypes.Contains(x.RowType))
            .OrderBy(x => x.SourceRowNumber)
            .ToArrayAsync(cancellationToken);

        var general = rows.SingleOrDefault(x => x.RowType == ReportRowType.General)
            ?? throw new InvalidOperationException("Raporun genel toplam satırı bulunamadı.");
        var categories = rows.Where(x => x.RowType == ReportRowType.CategorySummary).ToArray();
        var products = rows.Where(x => x.RowType == ReportRowType.ProductSummary).ToArray();
        var stores = rows.Where(x => x.RowType == ReportRowType.StoreProduct).ToArray();
        var categoryByStock = stores
            .GroupBy(ReportSnapshot.StockKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => ReportSnapshot.CategoryKey(group.First()), StringComparer.OrdinalIgnoreCase);

        var snapshot = new ReportSnapshot
        {
            Import = ToOption(importItem)!,
            General = general,
            Categories = categories,
            ProductsByCategory = products
                .GroupBy(product => categoryByStock.GetValueOrDefault(ReportSnapshot.StockKey(product), "(kategori-yok)"), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => (IReadOnlyList<ReportRowRecord>)x.ToArray(), StringComparer.OrdinalIgnoreCase),
            StoresByProduct = stores.GroupBy(ReportSnapshot.ProductKey)
                .ToDictionary(x => x.Key, x => (IReadOnlyList<ReportRowRecord>)x.ToArray(), StringComparer.OrdinalIgnoreCase),
            StoreProducts = stores
        };

        memoryCache.Set(cacheKey, snapshot, TimeSpan.FromMinutes(20));
        return snapshot;
    }

    private static ReportImportOption? ToOption(ReportImportRecord? item) => item is null
        ? null
        : new(item.Id, item.PeriodType, item.StartDate, item.EndDate, item.OriginalFileName);
}
