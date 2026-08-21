namespace OzkFireTakibiClient.Src.Data.Entities;

/// <summary>
/// Bir mağaza özeti veya mağaza × kategori rapor satırı için açılan mazeret talebi.
/// </summary>
public class ExcuseRequestEntity : BaseEntity<long>
{
    public long ReportRowId { get; set; }
    public ExcuseSource Source { get; set; }
    public string Title { get; set; } = default!;
    public string? RequestNote { get; set; }
    public int? RequestedByUserId { get; set; }
    public decimal? ThresholdRate { get; set; }
    public ExcuseStatus Status { get; set; } = ExcuseStatus.Open;
    public ExcuseStatus? StatusBeforeSuperseded { get; set; }
    public long? SupersededByReportImportId { get; set; }
    public DateTime? RespondedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }

    public ReportRowEntity ReportRow { get; set; } = default!;
    public UserEntity? RequestedByUser { get; set; }
    public ICollection<ExcuseEntryEntity> Entries { get; set; } = new List<ExcuseEntryEntity>();
}

public enum ExcuseSource
{
    Automatic,
    Manual
}

public enum ExcuseStatus
{
    Open,
    Answered,
    RevisionRequested,
    Approved,
    Superseded
}
