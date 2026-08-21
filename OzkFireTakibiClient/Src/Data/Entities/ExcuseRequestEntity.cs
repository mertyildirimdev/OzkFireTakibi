namespace OzkFireTakibiClient.Src.Data.Entities;

/// <summary>
/// Aylık kategori fire büyüklüğünü aşan mağaza × kategori satırı için açılan mazeret talebi.
/// </summary>
public class ExcuseRequestEntity : BaseEntity<long>
{
    public long ReportImportId { get; set; }
    public long ReportRowId { get; set; }
    public int StoreNumber { get; set; }
    public string StoreName { get; set; } = default!;
    public string CategoryCode { get; set; } = default!;
    public string CategoryName { get; set; } = default!;
    public decimal CategoryAverageWasteRate { get; set; }
    public decimal StoreWasteRate { get; set; }
    public decimal ThresholdWasteRate { get; set; }
    public decimal DeviationPercent { get; set; }
    public ExcuseStatus Status { get; set; } = ExcuseStatus.Open;
    public ExcuseStatus? StatusBeforeSuperseded { get; set; }
    public long? SupersededByReportImportId { get; set; }
    public DateTime? RespondedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }

    public ReportImportEntity ReportImport { get; set; } = default!;
    public ICollection<ExcuseEntryEntity> Entries { get; set; } = new List<ExcuseEntryEntity>();
}

public enum ExcuseStatus
{
    Open,
    Answered,
    RevisionRequested,
    Approved,
    Superseded
}
