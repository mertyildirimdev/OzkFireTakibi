namespace OzkFireTakibiClient.Src.Data.Entities;

public class ReportRowEntity : BaseEntity<long>
{
    public long ReportImportId { get; set; }
    public int SourceRowNumber { get; set; }
    public ReportRowType RowType { get; set; }
    public int SourceReportId { get; set; }
    public string SourceReportType { get; set; } = default!;
    public int? StoreNumber { get; set; }
    public string? StoreName { get; set; }
    public string? CategoryCode { get; set; }
    public string? CategoryName { get; set; }
    public string? StockCode { get; set; }
    public string? StockName { get; set; }
    public string? AlternativeName { get; set; }
    public string? CostGroupType { get; set; }
    public string? CostGroupCode { get; set; }
    public decimal? PurchaseGroupValueFactor { get; set; }
    public decimal? PurchaseStockValueFactor { get; set; }
    public decimal? OpeningQuantity { get; set; }
    public decimal? OpeningAmount { get; set; }
    public decimal? CompanyPurchaseQuantity { get; set; }
    public decimal? CompanyPurchaseAmount { get; set; }
    public decimal? WarehouseTransferInQuantity { get; set; }
    public decimal? WarehouseTransferInAmount { get; set; }
    public decimal? WarehouseTransferOutQuantity { get; set; }
    public decimal? WarehouseTransferOutAmount { get; set; }
    public decimal? StoreSalesQuantity { get; set; }
    public decimal? StoreSalesAmount { get; set; }
    public decimal? CostOfSales { get; set; }
    public decimal? WasteRate { get; set; }
    public decimal? WasteQuantity { get; set; }
    public decimal? WasteAmount { get; set; }
    public decimal? ClosingQuantity { get; set; }
    public decimal? ClosingAmount { get; set; }
    public decimal? ProfitAmount { get; set; }
    public decimal? ProfitRate { get; set; }
    public decimal? CategoryProfitRate { get; set; }
    public decimal? CategoryWasteRate { get; set; }

    public ReportImportEntity ReportImport { get; set; } = default!;
}

public enum ReportRowType
{
    General,
    CategorySummary,
    StoreSummary,
    StoreCategory,
    ProductSummary,
    StoreProduct
}
