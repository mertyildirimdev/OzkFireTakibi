using System.Security.Claims;

namespace OzkFireTakibiClient.Src.Authorization;

/// <summary>
/// ClaimsPrincipal üzerinde kimlik doğrulama ve kullanıcı bilgisi çıkarma için genişletme metotları.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Kullanıcının oturum açmış olduğunu doğrular; değilse UnauthorizedAccessException fırlatır.
    /// </summary>
    public static void EnsureAuthenticated(this ClaimsPrincipal user, string? message = null)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException(message ?? "Bu işlem için oturum açılmalıdır.");
        }
    }

    /// <summary>
    /// Oturum açmış kullanıcının sayısal kimliğini (Id) döndürür.
    /// </summary>
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Kullanıcı kimliği doğrulanamadı.");
    }
}
