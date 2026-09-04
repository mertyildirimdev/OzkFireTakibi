using System.Globalization;
using OzkFireTakibi.Dashboard.Data.Entities;

namespace OzkFireTakibi.Dashboard.Models;

public sealed record ReportImportOption(long Id, ReportPeriodType PeriodType, DateOnly StartDate, DateOnly EndDate, string OriginalFileName);

public sealed record ReportPeriodOption(long Id, DateOnly EndDate, ReportImportOption? Monthly, ReportImportOption? Cumulative)
{
    public string Label => EndDate.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("tr-TR"));
}

public sealed class ReportSnapshot
{
    public required ReportImportOption Import { get; init; }
    public required IReadOnlyList<ReportRowEntity> Rows { get; init; }
    public required ReportRowEntity General { get; init; }
    public required IReadOnlyList<ReportRowEntity> Categories { get; init; }
    public required IReadOnlyDictionary<string, IReadOnlyList<ReportRowEntity>> ProductsByCategory { get; init; }
    public required IReadOnlyDictionary<string, IReadOnlyList<ReportRowEntity>> StoresByProduct { get; init; }
    public required IReadOnlyList<ReportRowEntity> StoreProducts { get; init; }

    public static string CategoryKey(ReportRowEntity row) => row.CategoryCode ?? row.CategoryName ?? "(kategori-yok)";
    public static string StockKey(ReportRowEntity row) => row.StockCode ?? row.StockName ?? "(urun-yok)";
    public static string ProductKey(ReportRowEntity row) => ProductKey(CategoryKey(row), row);
    public static string ProductKey(string categoryKey, ReportRowEntity row) => $"{categoryKey}|{StockKey(row)}";
}

public enum ColumnDataType { Text, Number, Percentage }
public enum ColumnComparisonScope { None, Summary, Category }

public sealed record ColumnDefinition(
    string Key,
    string Label,
    ColumnDataType DataType,
    Func<ReportRowEntity, object?> Value,
    bool IsDefault = false,
    ColumnComparisonScope ComparisonScope = ColumnComparisonScope.None,
    Func<ReportRowEntity, object?>? ComparisonValue = null)
{
    public string ValueKey(ReportRowEntity row)
    {
        var value = Value(row);
        return value switch
        {
            null => string.Empty,
            decimal number => number.ToString(CultureInfo.InvariantCulture),
            int number => number.ToString(CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };
    }

    public string Format(ReportRowEntity row) => FormatValue(Value(row));
    public object? ValueForComparison(ReportRowEntity row) => (ComparisonValue ?? Value)(row);

    public string FormatValue(object? value)
    {
        return value switch
        {
            null => "–",
            decimal number when DataType == ColumnDataType.Percentage => $"% {number:N2}",
            decimal number => number.ToString("N2"),
            int number => number.ToString("N0"),
            _ => Convert.ToString(value, CultureInfo.CurrentCulture) ?? "–"
        };
    }
}

public sealed record DistinctValue(string Key, string Label, int Count);
public enum SortDirection { Ascending, Descending }
public sealed record SortRule(string ColumnKey, SortDirection Direction);
public sealed record ColumnFilterChange(string ColumnKey, IReadOnlySet<string>? SelectedValues);

public static class ReportColumnCatalog
{
    public static readonly IReadOnlyList<ColumnDefinition> All =
    [
        new("storeNumber", "Depo No", ColumnDataType.Number, row => row.StoreNumber),
        new("storeName", "Depo Adı", ColumnDataType.Text, row => row.StoreName),
        new("categoryCode", "Kategori Kodu", ColumnDataType.Text, row => row.CategoryCode),
        new("categoryName", "Kategori İsmi", ColumnDataType.Text, row => row.CategoryName),
        new("stockCode", "Stok Kodu", ColumnDataType.Text, row => row.StockCode),
        new("stockName", "Stok İsmi", ColumnDataType.Text, row => row.StockName),
        new("alternativeName", "Alternatif İsim", ColumnDataType.Text, row => row.AlternativeName),
        new("costGroupType", "Maliyet Grup Tipi", ColumnDataType.Text, row => row.CostGroupType),
        new("costGroupCode", "Maliyet Grup Kodu", ColumnDataType.Text, row => row.CostGroupCode),
        Metric("purchaseGroupValueFactor", "Satın Alma Grubu Değer Çarpanı", ColumnDataType.Number, row => row.PurchaseGroupValueFactor),
        Metric("purchaseStockValueFactor", "Satın Alma Stok Değer Çarpanı", ColumnDataType.Number, row => row.PurchaseStockValueFactor),
        Metric("openingQuantity", "Dönem Başı Miktar", ColumnDataType.Number, row => row.OpeningQuantity),
        Metric("openingAmount", "Dönem Başı Tutar", ColumnDataType.Number, row => row.OpeningAmount),
        Metric("companyPurchaseQuantity", "Firma Alış Miktar", ColumnDataType.Number, row => row.CompanyPurchaseQuantity),
        Metric("companyPurchaseAmount", "Firma Alış Tutar", ColumnDataType.Number, row => row.CompanyPurchaseAmount),
        Metric("warehouseTransferInQuantity", "Depo Sevk Alış Miktar", ColumnDataType.Number, row => row.WarehouseTransferInQuantity),
        Metric("warehouseTransferInAmount", "Depo Sevk Alış Tutar", ColumnDataType.Number, row => row.WarehouseTransferInAmount),
        Metric("warehouseTransferOutQuantity", "Depo Sevk Satış Miktar", ColumnDataType.Number, row => row.WarehouseTransferOutQuantity),
        Metric("warehouseTransferOutAmount", "Depo Sevk Satış Tutar", ColumnDataType.Number, row => row.WarehouseTransferOutAmount),
        Metric("storeSalesQuantity", "Mağaza Satış Miktar", ColumnDataType.Number, row => row.StoreSalesQuantity),
        Metric("storeSalesAmount", "Mağaza Satış Tutar", ColumnDataType.Number, row => row.StoreSalesAmount, true),
        Metric("costOfSales", "Satış Maliyeti", ColumnDataType.Number, row => row.CostOfSales, true),
        Metric("wasteRate", "Fire Oranı", ColumnDataType.Percentage, row => row.WasteRate, true),
        Metric("wasteQuantity", "Fire Miktarı", ColumnDataType.Number, row => row.WasteQuantity),
        Metric("wasteAmount", "Fire Tutarı", ColumnDataType.Number, row => row.WasteAmount, true),
        Metric("closingQuantity", "Dönem Sonu Miktar", ColumnDataType.Number, row => row.ClosingQuantity),
        Metric("closingAmount", "Dönem Sonu Tutar", ColumnDataType.Number, row => row.ClosingAmount),
        Metric("profitAmount", "Kar Tutar", ColumnDataType.Number, row => row.ProfitAmount, true),
        Metric("profitRate", "Kar Oranı", ColumnDataType.Percentage, row => row.ProfitRate, true),
        Metric(
            "categoryProfitRate",
            "Kategori Kar Oranı",
            ColumnDataType.Percentage,
            row => row.CategoryProfitRate,
            comparisonScope: ColumnComparisonScope.Category,
            comparisonValue: row => row.ProfitRate),
        Metric(
            "categoryWasteRate",
            "Kategori Fire Oranı",
            ColumnDataType.Percentage,
            row => row.CategoryWasteRate,
            comparisonScope: ColumnComparisonScope.Category,
            comparisonValue: row => row.WasteRate)
    ];

    private static ColumnDefinition Metric(
        string key,
        string label,
        ColumnDataType dataType,
        Func<ReportRowEntity, object?> value,
        bool isDefault = false,
        ColumnComparisonScope comparisonScope = ColumnComparisonScope.Summary,
        Func<ReportRowEntity, object?>? comparisonValue = null)
        => new(key, label, dataType, value, isDefault, comparisonScope, comparisonValue);
}
