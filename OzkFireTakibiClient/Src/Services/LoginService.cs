namespace OzkFireTakibiClient.Src.Services;

using OzkFireTakibiClient.Src.Data.Entities;
using OzkFireTakibiClient.Src.Authorization;

/// <summary>
/// Kullanıcı oturum açma, oturum kapatma ve oturum durumu sorgulama işlemlerini yöneten servis.
/// </summary>
public class LoginService(UserService userService, CustomStateProvider customStateProvider)
{
    /// <summary>
    /// Geçerli oturum açmış kullanıcı varlığı
    /// </summary>
    public UserEntity? CurrentUser => customStateProvider.CurrentUser;

    /// <summary>
    /// Kullanıcının oturum açmış olup olmadığını belirtir
    /// </summary>
    public bool IsAuthenticated => customStateProvider.CurrentUser != null;

    /// <summary>
    /// E-posta ve şifre ile kullanıcı girişi yapar ve oturum durumunu günceller.
    /// </summary>
    /// <param name="email">Kullanıcı e-posta adresi</param>
    /// <param name="password">Kullanıcı şifresi</param>
    /// <param name="rememberMe">Oturumun tarayıcıda hatırlanıp hatırlanmayacağı</param>
    /// <returns>Giriş başarılı ise true, aksi halde false</returns>
    public async Task<bool> LoginAsync(string email, string password, bool rememberMe = false)
    {
        var user = await userService.LoginAsync(email, password);
        if (user != null)
        {
            await customStateProvider.MarkUserAsAuthenticatedAsync(user, rememberMe);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Kullanıcı oturumunu asenkron olarak sonlandırır.
    /// </summary>
    public async Task LogoutAsync()
    {
        await customStateProvider.MarkUserAsLoggedOutAsync();
    }

    /// <summary>
    /// Kullanıcı oturumunu sonlandırır (fire-and-forget wrapper).
    /// </summary>
    public void Logout()
    {
        _ = LogoutAsync();
    }
}

