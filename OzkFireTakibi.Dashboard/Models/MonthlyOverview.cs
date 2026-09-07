using OzkFireTakibi.Dashboard.Data.Entities;

namespace OzkFireTakibi.Dashboard.Models;

public sealed record OverviewReportGroup(
    string Label,
    ReportPeriodOption? Period,
    ReportRowEntity? Summary,
    ReportRowEntity? PreviousSummary,
    decimal? BenchmarkWasteRate,
    int AttentionStoreCount);

public sealed record OverviewStoreReport(
    string GroupLabel, long PeriodId, ReportRowEntity Row, long? RequestId, ExcuseStatus? Status);

public sealed record OverviewStore(int StoreNumber, string StoreName, IReadOnlyList<OverviewStoreReport> Reports);

public sealed record MonthlyOverview(
    DateOnly Month, bool IsStoreScoped, IReadOnlyList<OverviewReportGroup> Groups, IReadOnlyList<OverviewStore> Stores)
{
    public int ReceivedCount => Groups.Count(x => x.Period?.Monthly is not null);
    public int MissingCount => Groups.Count - ReceivedCount;
    public int ComparableCount => Groups.Count(x => x.Summary?.WasteRate is not null && x.PreviousSummary?.WasteRate is not null);
    public int ImprovedCount => Groups.Count(x => x.Summary?.WasteRate is { } current &&
        x.PreviousSummary?.WasteRate is { } previous && Math.Abs(current) < Math.Abs(previous));
    public int WorsenedCount => Groups.Count(x => x.Summary?.WasteRate is { } current &&
        x.PreviousSummary?.WasteRate is { } previous && Math.Abs(current) > Math.Abs(previous));
}
