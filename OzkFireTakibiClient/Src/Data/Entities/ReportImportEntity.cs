namespace OzkFireTakibiClient.Src.Data.Entities;

public class ReportImportEntity : BaseEntity<long>
{
    public long ReportPeriodId { get; set; }
    public ReportScope Scope { get; set; }
    public ReportPeriodType PeriodType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string OriginalFileName { get; set; } = default!;
    public string FileHash { get; set; } = default!;
    public bool IsActive { get; set; }
    public int UploadedByUserId { get; set; }
    public int TotalRowCount { get; set; }
    public int GeneralRowCount { get; set; }
    public int CategorySummaryRowCount { get; set; }
    public int StoreSummaryRowCount { get; set; }
    public int StoreCategoryRowCount { get; set; }
    public int ProductSummaryRowCount { get; set; }
    public int StoreProductRowCount { get; set; }

    public ReportPeriodEntity ReportPeriod { get; set; } = default!;
    public UserEntity UploadedByUser { get; set; } = default!;
    public ICollection<ReportRowEntity> Rows { get; set; } = new List<ReportRowEntity>();
}

public enum ReportScope
{
    Delicatessen,
    NutsAndDriedFruit
}

public enum ReportPeriodType
{
    Monthly,
    Cumulative
}
