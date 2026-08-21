namespace OzkFireTakibiClient.Src.Services;

using OzkFireTakibiClient.Src.Data;
using OzkFireTakibiClient.Src.Data.Entities;

/// <summary>
/// Kullanıcı oturum açma, oturum kapatma ve oturum durumu sorgulama işlemlerini yöneten servis.
/// </summary>
public class LoginService(AppDbContext dbContext, UserService userService, CustomStateProvider customStateProvider) : BaseService(dbContext)
{
    private readonly UserService _userService = userService;
    private readonly CustomStateProvider _customStateProvider = customStateProvider;

    /// <summary>
    /// Geçerli oturum açmış kullanıcı varlığı
    /// </summary>
    public UserEntity? CurrentUser => _customStateProvider.CurrentUser;

    /// <summary>
    /// Kullanıcının oturum açmış olup olmadığını belirtir
    /// </summary>
    public bool IsAuthenticated => _customStateProvider.CurrentUser != null;

    /// <summary>
    /// E-posta ve şifre ile kullanıcı girişi yapar ve oturum durumunu günceller.
    /// </summary>
    /// <param name="email">Kullanıcı e-posta adresi</param>
    /// <param name="password">Kullanıcı şifresi</param>
    /// <param name="rememberMe">Oturumun tarayıcıda hatırlanıp hatırlanmayacağı</param>
    /// <returns>Giriş başarılı ise true, aksi halde false</returns>
    public async Task<bool> LoginAsync(string email, string password, bool rememberMe = false)
    {
        var user = await _userService.LoginAsync(email, password);
        if (user != null)
        {
            await _customStateProvider.MarkUserAsAuthenticatedAsync(user, rememberMe);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Kullanıcı oturumunu asenkron olarak sonlandırır.
    /// </summary>
    public async Task LogoutAsync()
    {
        await _customStateProvider.MarkUserAsLoggedOutAsync();
    }

    /// <summary>
    /// Kullanıcı oturumunu sonlandırır (fire-and-forget wrapper).
    /// </summary>
    public void Logout()
    {
        _ = LogoutAsync();
    }
}

