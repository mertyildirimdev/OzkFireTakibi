namespace OzkFireTakibiClient.Src;

using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.Extensions.DependencyInjection;
using OzkFireTakibiClient.Src.Data.Entities;
using OzkFireTakibiClient.Src.Services;

/// <summary>
/// Blazor uygulaması için özel kimlik doğrulama durum sağlayıcısı (AuthenticationStateProvider).
/// Tarayıcı oturum/yerel depolaması üzerinden oturum kalıcılığı ve ClaimsPrincipal yönetimini sağlar.
/// </summary>
public class CustomStateProvider(
    ProtectedLocalStorage protectedLocalStorage,
    ProtectedSessionStorage protectedSessionStorage,
    IServiceProvider serviceProvider) : AuthenticationStateProvider
{
    private const string StorageKey = "auth_session";
    private readonly ProtectedLocalStorage _protectedLocalStorage = protectedLocalStorage;
    private readonly ProtectedSessionStorage _protectedSessionStorage = protectedSessionStorage;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private UserEntity? _currentUser;
    private bool _hasAttemptedRestore;

    /// <summary>
    /// Geçerli oturum açmış kullanıcı bilgisi (oturum yoksa null)
    /// </summary>
    public UserEntity? CurrentUser => _currentUser;

    /// <summary>
    /// Mevcut kimlik doğrulama durumunu (AuthenticationState) döndürür.
    /// Kullanıcı henüz yüklenmemişse yerel depolamadaki oturumu geri yüklemeyi dener.
    /// </summary>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_currentUser == null && !_hasAttemptedRestore)
        {
            await TryRestoreSessionAsync();
        }

        var identity = _currentUser != null
            ? CreateIdentity(_currentUser)
            : new ClaimsIdentity();

        var user = new ClaimsPrincipal(identity);
        return new AuthenticationState(user);
    }

    /// <summary>
    /// Kullanıcıyı başarılı giriş sonrası oturum açmış olarak işaretler ve durumu tüm bileşenlere bildirir.
    /// </summary>
    /// <param name="user">Giriş yapan kullanıcı varlığı</param>
    /// <param name="rememberMe">Oturumun tarayıcı kapatıldıktan sonra da korunup korunmayacağı</param>
    public async Task MarkUserAsAuthenticatedAsync(UserEntity user, bool rememberMe)
    {
        _currentUser = user;
        _hasAttemptedRestore = true;

        if (rememberMe)
        {
            try { await _protectedSessionStorage.DeleteAsync(StorageKey); }
            catch { }

            try { await _protectedLocalStorage.SetAsync(StorageKey, CreateSession(user.Id, DateTime.UtcNow.AddDays(30))); }
            catch { /* Tarayıcı depolama hatası oluşursa oturum yine de bellekte açılmış olur. */ }
        }
        else
        {
            try { await _protectedLocalStorage.DeleteAsync(StorageKey); }
            catch { }

            try { await _protectedSessionStorage.SetAsync(StorageKey, CreateSession(user.Id, DateTime.UtcNow.AddHours(12))); }
            catch { /* Tarayıcı depolama hatası oluşursa oturum yine de bellekte açılmış olur. */ }
        }

        var identity = CreateIdentity(user);
        var principal = new ClaimsPrincipal(identity);

        // Kimlik doğrulama durumunun değiştiğini Blazor altyapısına bildir
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
    }

    /// <summary>
    /// Kullanıcı oturumunu kapatır, yerel depolamadaki oturumu siler ve durumu günceller.
    /// </summary>
    public async Task MarkUserAsLoggedOutAsync()
    {
        _currentUser = null;
        _hasAttemptedRestore = true;

        await ClearStoredSessionsAsync();

        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymousUser)));
    }

    /// <summary>
    /// Önce sekmeye ait sessionStorage, ardından kalıcı localStorage içindeki oturumu geri yüklemeyi dener.
    /// </summary>
    private async Task TryRestoreSessionAsync()
    {
        try
        {
            var sessionResult = await _protectedSessionStorage.GetAsync<AuthSession>(StorageKey);
            var session = sessionResult.Success ? sessionResult.Value : null;

            if (session is null)
            {
                var localResult = await _protectedLocalStorage.GetAsync<AuthSession>(StorageKey);
                session = localResult.Success ? localResult.Value : null;
            }

            if (session is not null && session.ExpiresAtUtc > DateTime.UtcNow)
            {
                using var scope = _serviceProvider.CreateScope();
                var userService = scope.ServiceProvider.GetRequiredService<UserService>();
                var user = await userService.GetUserByIdAsync(session.UserId);
                if (user is not null)
                {
                    _currentUser = user;
                    _hasAttemptedRestore = true;
                    return;
                }
            }

            if (session is not null)
            {
                // Süresi dolmuş veya geçersiz kullanıcı ise iki depodan da temizle.
                await ClearStoredSessionsAsync();
            }

            _hasAttemptedRestore = true;
        }
        catch (InvalidOperationException)
        {
            // Prerendering aşamasında JSInterop çağrılamaz, bu durum normaldir.
        }
        catch
        {
            _hasAttemptedRestore = true;
            await ClearStoredSessionsAsync();
        }
    }

    private async Task ClearStoredSessionsAsync()
    {
        try { await _protectedLocalStorage.DeleteAsync(StorageKey); }
        catch { }

        try { await _protectedSessionStorage.DeleteAsync(StorageKey); }
        catch { }
    }

    private static AuthSession CreateSession(int userId, DateTime expiresAtUtc) => new()
    {
        UserId = userId,
        ExpiresAtUtc = expiresAtUtc
    };

    /// <summary>
    /// Kullanıcı varlığından ClaimsIdentity nesnesi oluşturur (Id, Name, Email, Role, StoreName talepleri eklenir).
    /// </summary>
    private static ClaimsIdentity CreateIdentity(UserEntity user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name ?? user.Email),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role ?? UserRole.User.ToString())
        };

        if (!string.IsNullOrEmpty(user.StoreName))
        {
            claims.Add(new Claim("StoreName", user.StoreName));
        }

        if (user.StoreNumber.HasValue)
        {
            claims.Add(new Claim("StoreNumber", user.StoreNumber.Value.ToString()));
        }

        return new ClaimsIdentity(claims, "CustomAuth");
    }
}

