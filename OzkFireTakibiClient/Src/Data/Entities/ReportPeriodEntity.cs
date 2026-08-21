namespace OzkFireTakibiClient.Src.Data.Entities;

public class ReportPeriodEntity : BaseEntity<long>
{
    public ReportScope Scope { get; set; }
    public DateOnly EndDate { get; set; }

    public ICollection<ReportImportEntity> Imports { get; set; } = new List<ReportImportEntity>();
}
