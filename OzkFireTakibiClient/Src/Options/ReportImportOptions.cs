namespace OzkFireTakibiClient.Src.Options;

public sealed class ReportImportOptions
{
    public const string SectionName = "ReportImport";

    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024;
    public int HistoryPageSize { get; set; } = 50;
}
