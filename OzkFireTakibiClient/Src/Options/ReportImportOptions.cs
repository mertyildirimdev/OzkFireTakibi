namespace OzkFireTakibiClient.Src.Options;

/// <summary>
/// Rapor yükleme ve geçmiş görüntüleme ile ilgili yapılandırma ayarlarını temsil eder.
/// </summary>
public sealed class ReportImportOptions
{
    /// <summary>
    /// appsettings.json dosyasındaki yapılandırma bölümünün adı ("ReportImport")
    /// </summary>
    public const string SectionName = "ReportImport";

    /// <summary>
    /// İzin verilen maksimum Excel dosya boyutu (bayt cinsinden, varsayılan: 20 MB)
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024;

    /// <summary>
    /// Rapor geçmişi ve dönem listelerinde tek sayfada listelenecek maksimum kayıt sayısı
    /// </summary>
    public int HistoryPageSize { get; set; } = 50;
}

