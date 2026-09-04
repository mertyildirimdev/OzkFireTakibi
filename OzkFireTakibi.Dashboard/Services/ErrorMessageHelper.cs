using OzkFireTakibi.Dashboard.Importing;

namespace OzkFireTakibi.Dashboard.Services;

/// <summary>
/// Exception türlerini kullanıcı dostu Türkçe hata mesajlarına dönüştürür.
/// </summary>
public static class ErrorMessageHelper
{
    /// <summary>
    /// Rapor işlemlerindeki hataları kullanıcı dostu mesaja çevirir.
    /// </summary>
    public static string GetReportErrorMessage(Exception exception) => exception switch
    {
        ReportImportValidationException => exception.Message,
        ReportNotFoundException => exception.Message,
        UnauthorizedAccessException => exception.Message,
        IOException => "Dosya okunurken bir hata oluştu. Dosyayı kapatıp yeniden deneyin.",
        _ => "Rapor işlenirken beklenmeyen bir hata oluştu."
    };

    /// <summary>
    /// Rapor detay sayfasındaki hataları kullanıcı dostu mesaja çevirir.
    /// </summary>
    public static string GetDetailErrorMessage(Exception exception) => exception switch
    {
        ReportNotFoundException => exception.Message,
        UnauthorizedAccessException => exception.Message,
        _ => "Rapor detayları yüklenirken beklenmeyen bir hata oluştu."
    };
}
