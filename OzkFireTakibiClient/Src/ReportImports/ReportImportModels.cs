using OzkFireTakibiClient.Src.Data.Entities;

namespace OzkFireTakibiClient.Src.ReportImports;

/// <summary>
/// Excel dosyasından başarıyla ayrıştırılmış ham rapor verisini ve metaverilerini temsil eder.
/// </summary>
public sealed class ParsedReport
{
    /// <summary>
    /// Dosya içeriğinin SHA256 karma özeti
    /// </summary>
    public required string FileHash { get; init; }

    /// <summary>
    /// Kategori kodu kümesinin teknik eşleştirme imzası.
    /// </summary>
    public required string CategorySignature { get; init; }

    /// <summary>
    /// Raporun dönem tipi (Aylık kesinleşen veya Kümülatif)
    /// </summary>
    public required ReportPeriodType PeriodType { get; init; }

    /// <summary>
    /// Raporun başlangıç tarihi
    /// </summary>
    public required DateOnly StartDate { get; init; }

    /// <summary>
    /// Raporun bitiş tarihi
    /// </summary>
    public required DateOnly EndDate { get; init; }

    /// <summary>
    /// Ayrıştırılan satırların listesi
    /// </summary>
    public required IReadOnlyList<ParsedReportRow> Rows { get; init; }

    /// <summary>
    /// Belirtilen satır türüne (ReportRowType) ait satır sayısını döndürür.
    /// </summary>
    public int Count(ReportRowType rowType) => Rows.Count(x => x.RowType == rowType);
}

/// <summary>
/// Excel dosyasından ayrıştırılmış tek bir veri satırını temsil eder.
/// </summary>
public sealed class ParsedReportRow
{
    public required int SourceRowNumber { get; init; }
    public required ReportRowType RowType { get; init; }
    public required int SourceReportId { get; init; }
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

/// <summary>
/// Yükleme öncesinde aylık ve kümülatif rapor çiftinin doğrulama ve önizleme özetini içerir.
/// </summary>
public sealed class ReportPairImportPreview
{
    public required DateOnly EndDate { get; init; }
    public required ReportPairFilePreview MonthlyReport { get; init; }
    public required ReportPairFilePreview CumulativeReport { get; init; }

    /// <summary>
    /// Dosyalardan en az birinin yeni veya güncellenmiş sürüm olup olmadığını belirtir.
    /// </summary>
    public bool HasChanges => !MonthlyReport.IsAlreadyActive || !CumulativeReport.IsAlreadyActive;
}


/// <summary>
/// Çift rapor yüklemesinde tek bir dosyanın (aylık veya kümülatif) önizleme bilgilerini içerir.
/// </summary>
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

/// <summary>
/// Rapor geçmişi listesinde tek bir rapor içe aktarım kaydını temsil eden DTO.
/// </summary>
public sealed class ReportImportHistoryItem
{
    public required long Id { get; init; }
    public required long ReportPeriodId { get; init; }
    public required string OriginalFileName { get; init; }
    public required ReportPeriodType PeriodType { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
    public required bool IsActive { get; init; }
    public required int TotalRowCount { get; init; }
    public required string UploadedBy { get; init; }
    public required DateTime ImportedAtUtc { get; init; }
}

/// <summary>
/// Bağlı rapor dönemleri listesinde bir dönemin aylık ve kümülatif rapor durumunu temsil eden DTO.
/// </summary>
public sealed class ReportPeriodOverviewItem
{
    public required long Id { get; init; }
    public required DateOnly EndDate { get; init; }
    public ReportPeriodFileItem? MonthlyReport { get; init; }
    public ReportPeriodFileItem? CumulativeReport { get; init; }

    /// <summary>
    /// Hem aylık hem de kümülatif raporun eksiksiz yüklü olup olmadığını belirtir.
    /// </summary>
    public bool IsComplete => MonthlyReport is not null && CumulativeReport is not null;
}

/// <summary>
/// Dönem genel bakışındaki dosya bilgisi.
/// </summary>
public sealed class ReportPeriodFileItem
{
    public required long ImportId { get; init; }
    public required string OriginalFileName { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
}

/// <summary>
/// Analizler sayfasında gösterilen aktif aylık rapor ve genel sonuçları.
/// </summary>
public sealed class MonthlyAnalysisItem
{
    public required long ImportId { get; init; }
    public required string OriginalFileName { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
    public decimal? StoreSalesAmount { get; init; }
    public decimal? CostOfSales { get; init; }
    public decimal? WasteRate { get; init; }
    public decimal? WasteQuantity { get; init; }
    public decimal? WasteAmount { get; init; }
    public decimal? ProfitRate { get; init; }
    public decimal? ProfitAmount { get; init; }

    /// <summary>
    /// Aynı rapor dönemindeki aktif kümülatif genel sonuç; karşılaştırma kapalıysa veya eşleşme yoksa null.
    /// </summary>
    public MonthlyAnalysisItem? Comparison { get; init; }
}

/// <summary>
/// Analizler sayfasındaki genel rapor tablosunun filtre, sayfalama ve karşılaştırma sonucunu içerir.
/// </summary>
public sealed class MonthlyAnalysisResult
{
    public required MonthlyAnalysisFilter Filters { get; init; }
    public required int PageNumber { get; init; }
    public required int PageSize { get; init; }
    public required int TotalRowCount { get; init; }
    public required IReadOnlyList<MonthlyAnalysisItem> Rows { get; init; }
    public required IReadOnlyList<int> AvailableYears { get; init; }
    public required bool HasComparisonData { get; init; }
    public required bool IncludeComparison { get; init; }

    public int TotalPageCount => Math.Max(1, (int)Math.Ceiling(TotalRowCount / (double)PageSize));
}

/// <summary>
/// Aktif aylık raporların genel sonuç listesine uygulanan filtreler.
/// </summary>
public sealed class MonthlyAnalysisFilter
{
    public string SearchText { get; set; } = string.Empty;
    public int? Year { get; set; }
    public int? Month { get; set; }
    public ReportDetailWasteFilter WasteFilter { get; set; }
    public ReportDetailComparisonFilter ComparisonFilter { get; set; }
    public decimal? MinimumWasteRate { get; set; }
    public decimal? MaximumWasteRate { get; set; }
    public MonthlyAnalysisSort Sort { get; set; }
}

/// <summary>
/// Analiz listesinin sunucu tarafında uygulanabilen sıralama seçenekleri.
/// </summary>
public enum MonthlyAnalysisSort
{
    NewestPeriod,
    OldestPeriod,
    WorstWasteRate,
    WorstWasteAmount,
    HighestSalesAmount
}

/// <summary>
/// Aylık ve kümülatif rapor çifti yükleme işleminin veritabanı kayıt sonucunu temsil eder.
/// </summary>
public sealed class ReportPairImportResult
{
    public required long ReportPeriodId { get; init; }
    public required long MonthlyImportId { get; init; }
    public required long CumulativeImportId { get; init; }
    public required bool MonthlyReportChanged { get; init; }
    public required bool CumulativeReportChanged { get; init; }

    public required int GeneratedExcuseCount { get; init; }
}

/// <summary>
/// Rapor detay sayfası için sorgulanan başlık, sayfalama ve satır verilerini içerir.
/// </summary>
public sealed class ReportDetailResult
{
    public required ReportDetailHeader Header { get; init; }
    public required ReportRowType RowType { get; init; }
    public required ReportDetailFilter Filters { get; init; }
    public required int PageNumber { get; init; }
    public required int PageSize { get; init; }
    public required int TotalRowCount { get; init; }
    public required IReadOnlyList<ReportDetailRowItem> Rows { get; init; }
    public required bool IncludeComparison { get; init; }

    /// <summary>
    /// Toplam sayfa sayısı
    /// </summary>
    public int TotalPageCount => Math.Max(1, (int)Math.Ceiling(TotalRowCount / (double)PageSize));
}

/// <summary>
/// Rapor detay tablosuna sunucu tarafında uygulanan sade ve kırılım duyarlı filtreler.
/// </summary>
public sealed class ReportDetailFilter
{
    public string SearchText { get; set; } = string.Empty;
    public string StoreText { get; set; } = string.Empty;
    public string CategoryText { get; set; } = string.Empty;
    public string ProductText { get; set; } = string.Empty;
    public ReportDetailWasteFilter WasteFilter { get; set; }
    public ReportDetailComparisonFilter ComparisonFilter { get; set; }
    public decimal? MinimumWasteRate { get; set; }
    public decimal? MaximumWasteRate { get; set; }
    public ReportDetailSort Sort { get; set; }
}

/// <summary>
/// Negatif fire oranı veya tutarı bulunan satırların filtrelenme biçimi.
/// </summary>
public enum ReportDetailWasteFilter
{
    All,
    Loss,
    NoLoss
}

/// <summary>
/// Aylık satırların eşleşen kümülatif satıra göre filtrelenme biçimi.
/// </summary>
public enum ReportDetailComparisonFilter
{
    All,
    WorseThanCumulative
}

/// <summary>
/// Rapor detay satırlarının sunucu tarafındaki sıralama seçenekleri.
/// </summary>
public enum ReportDetailSort
{
    SourceOrder,
    WorstWasteRate,
    WorstWasteAmount,
    HighestSalesAmount
}

/// <summary>
/// Rapor detay görünümünün üst bilgi ve özet kartı verilerini içerir.
/// </summary>
public sealed class ReportDetailHeader
{
    public required long Id { get; init; }
    public required long ReportPeriodId { get; init; }
    public required string OriginalFileName { get; init; }
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

    /// <summary>
    /// Belirtilen satır hiyerarşisi türündeki satır sayısını döndürür.
    /// </summary>
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

/// <summary>
/// Aylık rapor ile karşılaştırılan aktif kümülatif raporun kaynak bilgilerini içerir.
/// </summary>
public sealed class ReportComparisonSource
{
    public required long ImportId { get; init; }
    public required string OriginalFileName { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
}

/// <summary>
/// Rapor detay tablosunda gösterilen tekil satır verisi ve varsa kümülatif eşleşme verisi.
/// </summary>
public sealed class ReportDetailRowItem
{
    public required long Id { get; init; }
    public required int SourceRowNumber { get; init; }
    public required int SourceReportId { get; init; }
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

    /// <summary>
    /// Kümülatif rapordaki eşleşen karşılaştırma satırı (varsa)
    /// </summary>
    public ReportDetailRowItem? Comparison { get; init; }
}

/// <summary>
/// Rapor silme işleminin sonucunu temsil eder.
/// </summary>
public sealed class ReportDeleteResult
{
    public required bool DeletedActiveVersion { get; init; }
    public required bool ReactivatedPreviousVersion { get; init; }
    public required bool DeletedEmptyReportPeriod { get; init; }
}

/// <summary>
/// Rapor doğrulama ve biçim hatalarında fırlatılan özel istisna sınıfı.
/// </summary>
public sealed class ReportImportValidationException : Exception
{
    public ReportImportValidationException(string message) : base(message)
    {
    }

    public ReportImportValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// İstenen rapor kimliği veritabanında bulunamadığında fırlatılan istisna sınıfı.
/// </summary>
public sealed class ReportNotFoundException : Exception
{
    public ReportNotFoundException(long reportImportId)
        : base($"{reportImportId} numaralı rapor bulunamadı.")
    {
    }
}

/// <summary>
/// Teknik enum değerleri için kullanıcı arayüzünde gösterilecek Türkçe metin yardımcı metotları.
/// </summary>
public static class ReportDisplayNames
{
    /// <summary>
    /// Rapor dönem tipini Türkçe metne çevirir ("Aylık kesinleşen", "Kümülatif karşılaştırma").
    /// </summary>
    public static string PeriodType(ReportPeriodType periodType) => periodType switch
    {
        ReportPeriodType.Monthly => "Aylık kesinleşen",
        ReportPeriodType.Cumulative => "Kümülatif karşılaştırma",
        _ => periodType.ToString()
    };

    /// <summary>
    /// Verilen dönem tipinin zıt karşılığını (Monthly -> Cumulative, Cumulative -> Monthly) Türkçe metin olarak döndürür.
    /// </summary>
    public static string Counterpart(ReportPeriodType periodType) => PeriodType(periodType switch
    {
        ReportPeriodType.Monthly => ReportPeriodType.Cumulative,
        ReportPeriodType.Cumulative => ReportPeriodType.Monthly,
        _ => throw new ArgumentOutOfRangeException(nameof(periodType), periodType, null)
    });

    /// <summary>
    /// Rapor satır hiyerarşi türünü Türkçe metne çevirir ("Genel", "Kategori özeti", "Mağaza özeti" vb.).
    /// </summary>
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
