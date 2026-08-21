namespace OzkFireTakibiClient.Src.Data.Entities;

/// <summary>
/// Belirli bir ürün grubu (Scope) ve dönem sonu tarihine (EndDate) ait rapor dönemini temsil eder.
/// Aylık kesinleşen ve kümülatif karşılaştırma raporları bu dönem altında gruplanır.
/// </summary>
public class ReportPeriodEntity : BaseEntity<long>
{
    /// <summary>
    /// Raporun ürün kapsamı (Şarküteri veya Kuruyemiş/Kuru Meyve)
    /// </summary>
    public ReportScope Scope { get; set; }

    /// <summary>
    /// Rapor döneminin bitiş tarihi
    /// </summary>
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Bu döneme ait yüklenmiş rapor sürümleri (aylık ve kümülatif import kayıtları)
    /// </summary>
    public ICollection<ReportImportEntity> Imports { get; set; } = new List<ReportImportEntity>();
}

