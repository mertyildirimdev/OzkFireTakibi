namespace OzkFireTakibiClient.Src.Data.Entities;

/// <summary>
/// Sisteme yüklenen tek bir Excel raporu içe aktarımını (import) ve metaverilerini temsil eder.
/// </summary>
public class ReportImportEntity : BaseEntity<long>
{
    /// <summary>
    /// Raporun bağlı olduğu dönem kimliği
    /// </summary>
    public long ReportPeriodId { get; set; }

    /// <summary>
    /// Raporun dönem türü (Monthly / Cumulative)
    /// </summary>
    public ReportPeriodType PeriodType { get; set; }

    /// <summary>
    /// Raporun kapsadığı başlangıç tarihi
    /// </summary>
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// Raporun kapsadığı bitiş tarihi
    /// </summary>
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Yüklenen orijinal dosya adı
    /// </summary>
    public string OriginalFileName { get; set; } = default!;

    /// <summary>
    /// Dosya içeriğinin SHA256 özeti (mükerrer yüklemeleri engellemek için)
    /// </summary>
    public string FileHash { get; set; } = default!;

    /// <summary>
    /// Bu raporun ilgili dönem ve tür için geçerli aktif sürüm olup olmadığını belirtir
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Raporu yükleyen kullanıcının kimliği
    /// </summary>
    public int UploadedByUserId { get; set; }

    /// <summary>
    /// Raporda bulunan toplam satır sayısı
    /// </summary>
    public int TotalRowCount { get; set; }

    /// <summary>
    /// Genel özet satırı sayısı (1 adet)
    /// </summary>
    public int GeneralRowCount { get; set; }

    /// <summary>
    /// Kategori özeti satırı sayısı (rpr_id = 2)
    /// </summary>
    public int CategorySummaryRowCount { get; set; }

    /// <summary>
    /// Mağaza özeti satırı sayısı (rpr_id = 3)
    /// </summary>
    public int StoreSummaryRowCount { get; set; }

    /// <summary>
    /// Mağaza × kategori detay satırı sayısı (rpr_id = 4)
    /// </summary>
    public int StoreCategoryRowCount { get; set; }

    /// <summary>
    /// Ürün özeti satırı sayısı (rpr_id = 5)
    /// </summary>
    public int ProductSummaryRowCount { get; set; }

    /// <summary>
    /// Mağaza × ürün detay satırı sayısı (rpr_id = 7)
    /// </summary>
    public int StoreProductRowCount { get; set; }

    /// <summary>
    /// Bağlı olduğu rapor dönemi navigasyon özelliği
    /// </summary>
    public ReportPeriodEntity ReportPeriod { get; set; } = default!;

    /// <summary>
    /// Raporu yükleyen kullanıcı navigasyon özelliği
    /// </summary>
    public UserEntity UploadedByUser { get; set; } = default!;

    /// <summary>
    /// Rapora ait detay veri satırları
    /// </summary>
    public ICollection<ReportRowEntity> Rows { get; set; } = new List<ReportRowEntity>();

}

/// <summary>
/// Raporun kapsadığı dönem tipi.
/// </summary>
public enum ReportPeriodType
{
    /// <summary>
    /// Aylık kesinleşen rapor (genellikle 1 aylık dönem)
    /// </summary>
    Monthly,

    /// <summary>
    /// Kümülatif karşılaştırma raporu (yılbaşından ilgili ay sonuna kadar olan dönem)
    /// </summary>
    Cumulative
}
