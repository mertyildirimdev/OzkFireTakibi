using OzkFireTakibi.Dashboard.Data.Entities;

namespace OzkFireTakibi.Dashboard.Models;

/// <summary>
/// Bir detay satırını Excel'deki aynı döneme ait doğru özet satırıyla eşleştirir.
/// </summary>
public sealed class ReportComparisonIndex
{
    private readonly ReportRowEntity _general;
    private readonly IReadOnlyDictionary<string, ReportRowEntity> _categories;
    private readonly IReadOnlyDictionary<string, ReportRowEntity> _products;

    public ReportComparisonIndex(ReportSnapshot snapshot)
    {
        _general = snapshot.General;
        _categories = snapshot.Categories
            .GroupBy(ReportSnapshot.CategoryKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        _products = snapshot.ProductsByCategory.Values
            .SelectMany(products => products)
            .GroupBy(ReportSnapshot.StockKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    public ReportRowEntity? FindFor(ReportRowEntity row, ColumnComparisonScope scope) => scope switch
    {
        ColumnComparisonScope.Summary => FindSummaryFor(row),
        ColumnComparisonScope.Category when row.RowType == ReportRowType.StoreProduct
            => _categories.GetValueOrDefault(ReportSnapshot.CategoryKey(row)),
        _ => null
    };

    private ReportRowEntity? FindSummaryFor(ReportRowEntity row) => row.RowType switch
    {
        ReportRowType.CategorySummary or ReportRowType.StoreSummary or ReportRowType.ProductSummary => _general,
        ReportRowType.StoreCategory => _categories.GetValueOrDefault(ReportSnapshot.CategoryKey(row)),
        ReportRowType.StoreProduct => _products.GetValueOrDefault(ReportSnapshot.StockKey(row)),
        _ => null
    };
}
