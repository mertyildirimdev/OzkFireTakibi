using OzkFireTakibiClient.Src.Data.Entities;

namespace OzkFireTakibiClient.Src.Excuses;

public sealed class ExcuseListResult
{
    public required IReadOnlyList<ExcuseListItem> Items { get; init; }
    public required int PageNumber { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }
    public required int OpenCount { get; init; }
    public required int AnsweredCount { get; init; }
    public required int RevisionRequestedCount { get; init; }
    public required int ApprovedCount { get; init; }
    public required bool IsStoreUser { get; init; }
    public int? StoreNumber { get; init; }
    public string? StoreName { get; init; }

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}

public sealed class ExcuseListItem
{
    public required long Id { get; init; }
    public required string Title { get; init; }
    public required ExcuseSource Source { get; init; }
    public required ReportRowType TargetRowType { get; init; }
    public required ReportScope Scope { get; init; }
    public required DateOnly PeriodEndDate { get; init; }
    public required int StoreNumber { get; init; }
    public required string StoreName { get; init; }
    public string? TargetCode { get; init; }
    public string? TargetName { get; init; }
    public decimal? BenchmarkRate { get; init; }
    public decimal? StoreRate { get; init; }
    public decimal? ThresholdRate { get; init; }
    public decimal? DeviationPercent { get; init; }
    public required ExcuseStatus Status { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
}

public sealed class ExcuseDetailResult
{
    public required long Id { get; init; }
    public required long ReportImportId { get; init; }
    public required string Title { get; init; }
    public required ExcuseSource Source { get; init; }
    public required ReportRowType TargetRowType { get; init; }
    public required ReportScope Scope { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
    public required int StoreNumber { get; init; }
    public required string StoreName { get; init; }
    public string? TargetCode { get; init; }
    public string? TargetName { get; init; }
    public string? RequestNote { get; init; }
    public string? RequestedBy { get; init; }
    public decimal? BenchmarkRate { get; init; }
    public decimal? StoreRate { get; init; }
    public decimal? ThresholdRate { get; init; }
    public decimal? DeviationPercent { get; init; }
    public decimal? CumulativeBenchmarkRate { get; init; }
    public decimal? CumulativeStoreRate { get; init; }
    public required ExcuseStatus Status { get; init; }
    public required IReadOnlyList<ExcuseEntryItem> Entries { get; init; }
    public required IReadOnlyList<ExcuseCategoryItem> TopCategories { get; init; }
    public required IReadOnlyList<ExcuseProductItem> TopProducts { get; init; }
    public required bool CanRespond { get; init; }
    public required bool CanReview { get; init; }
}

public sealed class ExcuseEntryItem
{
    public required ExcuseEntryType EntryType { get; init; }
    public ExcuseReasonType? ReasonType { get; init; }
    public required string Message { get; init; }
    public required string CreatedBy { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
}

public sealed class ExcuseCategoryItem
{
    public string? CategoryCode { get; init; }
    public string? CategoryName { get; init; }
    public decimal? WasteRate { get; init; }
    public decimal? WasteAmount { get; init; }
}

public sealed class ExcuseProductItem
{
    public string? StockCode { get; init; }
    public string? StockName { get; init; }
    public decimal? WasteRate { get; init; }
    public decimal? WasteAmount { get; init; }
}

public sealed class ExcuseStoreItem
{
    public required int StoreNumber { get; init; }
    public required string StoreName { get; init; }
    public required bool IsExcuseEligible { get; init; }
}

public static class ExcuseDisplayNames
{
    public static string Status(ExcuseStatus status) => status switch
    {
        ExcuseStatus.Open => "Açık",
        ExcuseStatus.Answered => "Yanıtlandı",
        ExcuseStatus.RevisionRequested => "Revizyon istendi",
        ExcuseStatus.Approved => "Onaylandı",
        ExcuseStatus.Superseded => "Geçersiz kaldı",
        _ => status.ToString()
    };

    public static string StatusCss(ExcuseStatus status) => status switch
    {
        ExcuseStatus.Open => "is-danger is-light",
        ExcuseStatus.Answered => "is-info is-light",
        ExcuseStatus.RevisionRequested => "is-warning is-light",
        ExcuseStatus.Approved => "is-success is-light",
        _ => "is-light"
    };

    public static string Source(ExcuseSource source) => source switch
    {
        ExcuseSource.Automatic => "Otomatik",
        ExcuseSource.Manual => "Manuel",
        _ => source.ToString()
    };

    public static string Target(ReportRowType rowType) => rowType switch
    {
        ReportRowType.StoreSummary => "Rapor geneli",
        ReportRowType.StoreCategory => "Alt kategori",
        ReportRowType.StoreProduct => "Ürün",
        _ => rowType.ToString()
    };

    public static string Reason(ExcuseReasonType reason) => reason switch
    {
        ExcuseReasonType.CountingDifference => "Sayım farkı",
        ExcuseReasonType.ShipmentOrWaybill => "Sevk / irsaliye problemi",
        ExcuseReasonType.SpoilageOrPhysicalWaste => "Bozulma veya fiziksel fire",
        ExcuseReasonType.IncorrectStockMovement => "Yanlış stok hareketi",
        ExcuseReasonType.Return => "İade",
        ExcuseReasonType.PackagingOrProduction => "Paketleme / üretim",
        ExcuseReasonType.Other => "Diğer",
        _ => reason.ToString()
    };

    public static string EntryType(ExcuseEntryType entryType) => entryType switch
    {
        ExcuseEntryType.StoreResponse => "Mağaza yanıtı",
        ExcuseEntryType.RevisionRequest => "Revizyon talebi",
        ExcuseEntryType.Approval => "Onay",
        _ => entryType.ToString()
    };
}

public sealed class ExcuseNotFoundException(long id)
    : Exception($"{id} numaralı mazeret talebi bulunamadı.");
