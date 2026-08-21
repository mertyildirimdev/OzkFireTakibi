using OzkFireTakibiClient.Src.Data.Entities;

namespace OzkFireTakibiClient.Src.ReportImports;

public sealed class ParsedReport
{
    public required string FileHash { get; init; }
    public required ReportScope Scope { get; init; }
    public required ReportPeriodType PeriodType { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
    public required IReadOnlyList<ParsedReportRow> Rows { get; init; }

    public int Count(ReportRowType rowType) => Rows.Count(x => x.RowType == rowType);
}

public sealed class ParsedReportRow
{
    public required int SourceRowNumber { get; init; }
    public required ReportRowType RowType { get; init; }
    public required string SourceReportType { get; init; }
    public int? StoreNumber { get; init; }
    public string? StoreName { get; init; }
    public string? CategoryCode { get; init; }
    public string? CategoryName { get; init; }
    public string? StockCode { get; set; }
    public string? StockName { get; init; }
    public string? AlternativeName { get; init; }
    public string? CostGroupType { get; init; }
    public string? CostGroupCode { get; init; }
    public decimal? PurchaseGroupValueFactor { get; init; }
    public decimal? PurchaseStockValueFactor { get; init; }
    public decimal? OpeningQuantity { get; init; }
    public decimal? OpeningAmount { get; init; }
    public decimal? CompanyPurchaseQuantity { get; init; }
    public decimal? CompanyPurchaseAmount { get; init; }
    public decimal? WarehouseTransferInQuantity { get; init; }
    public decimal? WarehouseTransferInAmount { get; init; }
    public decimal? WarehouseTransferOutQuantity { get; init; }
    public decimal? WarehouseTransferOutAmount { get; init; }
    public decimal? StoreSalesQuantity { get; init; }
    public decimal? StoreSalesAmount { get; init; }
    public decimal? CostOfSales { get; init; }
    public decimal? WasteRate { get; init; }
    public decimal? WasteQuantity { get; init; }
    public decimal? WasteAmount { get; init; }
    public decimal? ClosingQuantity { get; init; }
    public decimal? ClosingAmount { get; init; }
    public decimal? ProfitAmount { get; init; }
    public decimal? ProfitRate { get; init; }
    public decimal? CategoryProfitRate { get; init; }
    public decimal? CategoryWasteRate { get; init; }
}

public sealed class ReportPairImportPreview
{
    public required ReportScope Scope { get; init; }
    public required DateOnly EndDate { get; init; }
    public required ReportPairFilePreview MonthlyReport { get; init; }
    public required ReportPairFilePreview CumulativeReport { get; init; }

    public bool HasChanges => !MonthlyReport.IsAlreadyActive || !CumulativeReport.IsAlreadyActive;
}

public sealed class ReportPairFilePreview
{
    public required string OriginalFileName { get; init; }
    public required ReportPeriodType PeriodType { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
    public required int TotalRowCount { get; init; }
    public required int GeneralRowCount { get; init; }
    public required int CategorySummaryRowCount { get; init; }
    public required int StoreSummaryRowCount { get; init; }
    public required int StoreCategoryRowCount { get; init; }
    public required int ProductSummaryRowCount { get; init; }
    public required int StoreProductRowCount { get; init; }
    public required bool IsAlreadyActive { get; init; }
    public required bool ReplacesActiveVersion { get; init; }
}

public sealed class ReportImportHistoryItem
{
    public required long Id { get; init; }
    public required long ReportPeriodId { get; init; }
    public required string OriginalFileName { get; init; }
    public required ReportScope Scope { get; init; }
    public required ReportPeriodType PeriodType { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
    public required bool IsActive { get; init; }
    public required int TotalRowCount { get; init; }
    public required string UploadedBy { get; init; }
    public required DateTime ImportedAtUtc { get; init; }
}

public sealed class ReportPeriodOverviewItem
{
    public required long Id { get; init; }
    public required ReportScope Scope { get; init; }
    public required DateOnly EndDate { get; init; }
    public ReportPeriodFileItem? MonthlyReport { get; init; }
    public ReportPeriodFileItem? CumulativeReport { get; init; }

    public bool IsComplete => MonthlyReport is not null && CumulativeReport is not null;
}

public sealed class ReportPeriodFileItem
{
    public required long ImportId { get; init; }
    public required string OriginalFileName { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
}

public sealed class ReportPairImportResult
{
    public required long ReportPeriodId { get; init; }
    public required long MonthlyImportId { get; init; }
    public required long CumulativeImportId { get; init; }
    public required bool MonthlyReportChanged { get; init; }
    public required bool CumulativeReportChanged { get; init; }
}

public sealed class ReportDetailResult
{
    public required ReportDetailHeader Header { get; init; }
    public required ReportRowType RowType { get; init; }
    public required string SearchText { get; init; }
    public required int PageNumber { get; init; }
    public required int PageSize { get; init; }
    public required int TotalRowCount { get; init; }
    public required IReadOnlyList<ReportDetailRowItem> Rows { get; init; }
    public required bool IncludeComparison { get; init; }

    public int TotalPageCount => Math.Max(1, (int)Math.Ceiling(TotalRowCount / (double)PageSize));
}

public sealed class ReportDetailHeader
{
    public required long Id { get; init; }
    public required long ReportPeriodId { get; init; }
    public required string OriginalFileName { get; init; }
    public required ReportScope Scope { get; init; }
    public required ReportPeriodType PeriodType { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
    public required bool IsActive { get; init; }
    public required int TotalRowCount { get; init; }
    public required int GeneralRowCount { get; init; }
    public required int CategorySummaryRowCount { get; init; }
    public required int StoreSummaryRowCount { get; init; }
    public required int StoreCategoryRowCount { get; init; }
    public required int ProductSummaryRowCount { get; init; }
    public required int StoreProductRowCount { get; init; }
    public required string UploadedBy { get; init; }
    public required DateTime ImportedAtUtc { get; init; }
    public ReportDetailRowItem? GeneralSummary { get; init; }
    public ReportComparisonSource? ComparisonSource { get; init; }

    public int Count(ReportRowType rowType) => rowType switch
    {
        ReportRowType.General => GeneralRowCount,
        ReportRowType.CategorySummary => CategorySummaryRowCount,
        ReportRowType.StoreSummary => StoreSummaryRowCount,
        ReportRowType.StoreCategory => StoreCategoryRowCount,
        ReportRowType.ProductSummary => ProductSummaryRowCount,
        ReportRowType.StoreProduct => StoreProductRowCount,
        _ => 0
    };
}

public sealed class ReportComparisonSource
{
    public required long ImportId { get; init; }
    public required string OriginalFileName { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
}

public sealed class ReportDetailRowItem
{
    public required long Id { get; init; }
    public required int SourceRowNumber { get; init; }
    public required string SourceReportType { get; init; }
    public int? StoreNumber { get; init; }
    public string? StoreName { get; init; }
    public string? CategoryCode { get; init; }
    public string? CategoryName { get; init; }
    public string? StockCode { get; init; }
    public string? StockName { get; init; }
    public string? AlternativeName { get; init; }
    public string? CostGroupType { get; init; }
    public string? CostGroupCode { get; init; }
    public decimal? PurchaseGroupValueFactor { get; init; }
    public decimal? PurchaseStockValueFactor { get; init; }
    public decimal? OpeningQuantity { get; init; }
    public decimal? OpeningAmount { get; init; }
    public decimal? CompanyPurchaseQuantity { get; init; }
    public decimal? CompanyPurchaseAmount { get; init; }
    public decimal? WarehouseTransferInQuantity { get; init; }
    public decimal? WarehouseTransferInAmount { get; init; }
    public decimal? WarehouseTransferOutQuantity { get; init; }
    public decimal? WarehouseTransferOutAmount { get; init; }
    public decimal? StoreSalesQuantity { get; init; }
    public decimal? StoreSalesAmount { get; init; }
    public decimal? CostOfSales { get; init; }
    public decimal? WasteRate { get; init; }
    public decimal? WasteQuantity { get; init; }
    public decimal? WasteAmount { get; init; }
    public decimal? ClosingQuantity { get; init; }
    public decimal? ClosingAmount { get; init; }
    public decimal? ProfitAmount { get; init; }
    public decimal? ProfitRate { get; init; }
    public decimal? CategoryProfitRate { get; init; }
    public decimal? CategoryWasteRate { get; init; }
    public ReportDetailRowItem? Comparison { get; init; }
}

public sealed class ReportDeleteResult
{
    public required bool DeletedActiveVersion { get; init; }
    public required bool ReactivatedPreviousVersion { get; init; }
    public required bool DeletedEmptyReportPeriod { get; init; }
}

public sealed class ReportImportValidationException : Exception
{
    public ReportImportValidationException(string message) : base(message)
    {
    }

    public ReportImportValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public sealed class ReportNotFoundException : Exception
{
    public ReportNotFoundException(long reportImportId)
        : base($"{reportImportId} numaralı rapor bulunamadı.")
    {
    }
}

public static class ReportDisplayNames
{
    public static string Scope(ReportScope scope) => scope switch
    {
        ReportScope.Delicatessen => "Şarküteri",
        ReportScope.NutsAndDriedFruit => "Kuruyemiş ve Kuru Meyve",
        _ => scope.ToString()
    };

    public static string PeriodType(ReportPeriodType periodType) => periodType switch
    {
        ReportPeriodType.Monthly => "Aylık kesinleşen",
        ReportPeriodType.Cumulative => "Kümülatif karşılaştırma",
        _ => periodType.ToString()
    };

    public static string Counterpart(ReportPeriodType periodType) => PeriodType(periodType switch
    {
        ReportPeriodType.Monthly => ReportPeriodType.Cumulative,
        ReportPeriodType.Cumulative => ReportPeriodType.Monthly,
        _ => throw new ArgumentOutOfRangeException(nameof(periodType), periodType, null)
    });

    public static string RowType(ReportRowType rowType) => rowType switch
    {
        ReportRowType.General => "Genel",
        ReportRowType.CategorySummary => "Kategori özeti",
        ReportRowType.StoreSummary => "Mağaza özeti",
        ReportRowType.StoreCategory => "Mağaza × kategori",
        ReportRowType.ProductSummary => "Ürün özeti",
        ReportRowType.StoreProduct => "Mağaza × ürün",
        _ => rowType.ToString()
    };
}
