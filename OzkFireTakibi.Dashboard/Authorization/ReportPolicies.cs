namespace OzkFireTakibi.Dashboard.Authorization;

/// <summary>
/// Rapor işlemleri için kullanılan yetkilendirme ilke (policy) adlarını tanımlar.
/// </summary>
public static class ReportPolicies
{
    /// <summary>
    /// Rapor yükleme yetkisi (Admin ve Moderator rolleri)
    /// </summary>
    public const string CanImportReports = "CanImportReports";

    /// <summary>
    /// Rapor silme yetkisi (Sadece Admin rolü)
    /// </summary>
    public const string CanDeleteReports = "CanDeleteReports";

    /// <summary>
    /// Mağazadan gelen mazeret yanıtını onaylama veya revizyona gönderme yetkisi.
    /// </summary>
    public const string CanReviewExcuses = "CanReviewExcuses";

    /// <summary>
    /// Alt detay satırları için manuel mazeret talebi oluşturma yetkisi.
    /// </summary>
    public const string CanRequestExcuses = "CanRequestExcuses";

    /// <summary>
    /// Mağazaların otomatik mazeret kapsamına dahil edilme durumunu yönetme yetkisi.
    /// </summary>
    public const string CanManageExcuseStores = "CanManageExcuseStores";
}
