namespace OzkFireTakibiClient.Src.Authorization;

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

    public const string CanReviewExcuses = "CanReviewExcuses";

    public const string CanManageExcuseStores = "CanManageExcuseStores";
}
