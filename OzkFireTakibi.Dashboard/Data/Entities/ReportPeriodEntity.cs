namespace OzkFireTakibi.Dashboard.Data.Entities;

/// <summary>
/// Aynı kategori kümesi ve dönem sonu tarihine ait rapor dönemini temsil eder.
/// Aylık kesinleşen ve kümülatif karşılaştırma raporları bu dönem altında gruplanır.
/// </summary>
public class ReportPeriodEntity : BaseEntity<long>
{
    /// <summary>
    /// Excel'deki sıralanmış kategori kodlarından üretilen teknik eşleştirme imzası.
    /// Kullanıcı arayüzünde gösterilmez.
    /// </summary>
    public string CategorySignature { get; set; } = default!;

    /// <summary>
    /// Rapor döneminin bitiş tarihi
    /// </summary>
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Bu döneme ait yüklenmiş rapor sürümleri (aylık ve kümülatif import kayıtları)
    /// </summary>
    public ICollection<ReportImportEntity> Imports { get; set; } = new List<ReportImportEntity>();
}
