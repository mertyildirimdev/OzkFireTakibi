using System.Globalization;
using OzkFireTakibi.Dashboard.Data;

namespace OzkFireTakibi.Dashboard.Models;

public sealed record ReportImportOption(long Id, ReportPeriodType PeriodType, DateOnly StartDate, DateOnly EndDate, string OriginalFileName);

public sealed record ReportPeriodOption(long Id, DateOnly EndDate, ReportImportOption? Monthly, ReportImportOption? Cumulative)
{
    public string Label => EndDate.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("tr-TR"));
}

public sealed class ReportSnapshot
{
    public required ReportImportOption Import { get; init; }
    public required ReportRowRecord General { get; init; }
    public required IReadOnlyList<ReportRowRecord> Categories { get; init; }
    public required IReadOnlyDictionary<string, IReadOnlyList<ReportRowRecord>> ProductsByCategory { get; init; }
    public required IReadOnlyDictionary<string, IReadOnlyList<ReportRowRecord>> StoresByProduct { get; init; }
    public required IReadOnlyList<ReportRowRecord> StoreProducts { get; init; }

    public static string CategoryKey(ReportRowRecord row) => row.CategoryCode ?? row.CategoryName ?? "(kategori-yok)";
    public static string StockKey(ReportRowRecord row) => row.StockCode ?? row.StockName ?? "(urun-yok)";
    public static string ProductKey(ReportRowRecord row) => ProductKey(CategoryKey(row), row);
    public static string ProductKey(string categoryKey, ReportRowRecord row) => $"{categoryKey}|{StockKey(row)}";
}

public enum ReportTreeLevel { General, Category, Product, Store }

public sealed record ReportTreeRow(string Key, ReportTreeLevel Level, ReportRowRecord Data, bool HasChildren, bool IsExpanded);

public enum ColumnDataType { Text, Number, Percentage }

public sealed record ColumnDefinition(
    string Key,
    string Label,
    ColumnDataType DataType,
    Func<ReportRowRecord, object?> Value,
    bool IsDefault = false)
{
    public string ValueKey(ReportRowRecord row)
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

    public string Format(ReportRowRecord row)
    {
        var value = Value(row);
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
        new("purchaseGroupValueFactor", "Satın Alma Grubu Değer Çarpanı", ColumnDataType.Number, row => row.PurchaseGroupValueFactor),
        new("purchaseStockValueFactor", "Satın Alma Stok Değer Çarpanı", ColumnDataType.Number, row => row.PurchaseStockValueFactor),
        new("openingQuantity", "Dönem Başı Miktar", ColumnDataType.Number, row => row.OpeningQuantity),
        new("openingAmount", "Dönem Başı Tutar", ColumnDataType.Number, row => row.OpeningAmount),
        new("companyPurchaseQuantity", "Firma Alış Miktar", ColumnDataType.Number, row => row.CompanyPurchaseQuantity),
        new("companyPurchaseAmount", "Firma Alış Tutar", ColumnDataType.Number, row => row.CompanyPurchaseAmount),
        new("warehouseTransferInQuantity", "Depo Sevk Alış Miktar", ColumnDataType.Number, row => row.WarehouseTransferInQuantity),
        new("warehouseTransferInAmount", "Depo Sevk Alış Tutar", ColumnDataType.Number, row => row.WarehouseTransferInAmount),
        new("warehouseTransferOutQuantity", "Depo Sevk Satış Miktar", ColumnDataType.Number, row => row.WarehouseTransferOutQuantity),
        new("warehouseTransferOutAmount", "Depo Sevk Satış Tutar", ColumnDataType.Number, row => row.WarehouseTransferOutAmount),
        new("storeSalesQuantity", "Mağaza Satış Miktar", ColumnDataType.Number, row => row.StoreSalesQuantity),
        new("storeSalesAmount", "Mağaza Satış Tutar", ColumnDataType.Number, row => row.StoreSalesAmount, true),
        new("costOfSales", "Satış Maliyeti", ColumnDataType.Number, row => row.CostOfSales, true),
        new("wasteRate", "Fire Oranı", ColumnDataType.Percentage, row => row.WasteRate, true),
        new("wasteQuantity", "Fire Miktarı", ColumnDataType.Number, row => row.WasteQuantity),
        new("wasteAmount", "Fire Tutarı", ColumnDataType.Number, row => row.WasteAmount, true),
        new("closingQuantity", "Dönem Sonu Miktar", ColumnDataType.Number, row => row.ClosingQuantity),
        new("closingAmount", "Dönem Sonu Tutar", ColumnDataType.Number, row => row.ClosingAmount),
        new("profitAmount", "Kar Tutar", ColumnDataType.Number, row => row.ProfitAmount, true),
        new("profitRate", "Kar Oranı", ColumnDataType.Percentage, row => row.ProfitRate, true),
        new("categoryProfitRate", "Kategori Kar Oranı", ColumnDataType.Percentage, row => row.CategoryProfitRate),
        new("categoryWasteRate", "Kategori Fire Oranı", ColumnDataType.Percentage, row => row.CategoryWasteRate)
    ];
}
