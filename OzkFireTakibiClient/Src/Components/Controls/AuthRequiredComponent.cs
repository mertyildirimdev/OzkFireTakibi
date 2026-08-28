using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using OzkFireTakibiClient.Src.Data.Entities;
using OzkFireTakibiClient.Src.Authorization;

namespace OzkFireTakibiClient.Src.Components.Controls;

/// <summary>
/// Oturum açmış kullanıcı gerektiren Razor bileşenleri için temel sınıf.
/// Kimlik doğrulaması yapılmamış istekleri otomatik olarak login sayfasına (dönüş URL'i ile birlikte) yönlendirir.
/// </summary>
public class AuthRequiredComponent : ComponentBase
{
    /// <summary>
    /// Üst bileşenden aktarılan kimlik doğrulama durumu görevi
    /// </summary>
    [CascadingParameter]
    private Task<AuthenticationState>? AuthStateTask { get; set; }

    /// <summary>
    /// Özel durum sağlayıcısı örneği
    /// </summary>
    [Inject]
    public CustomStateProvider StateProvider { get; set; } = default!;

    /// <summary>
    /// Sayfa yönlendirme yöneticisi
    /// </summary>
    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    /// <summary>
    /// Geçerli oturum açmış kullanıcı
    /// </summary>
    public UserEntity? CurrentUser => StateProvider.CurrentUser;

    /// <summary>
    /// Kullanıcının rolü
    /// </summary>
    public string UserRole => CurrentUser?.Role ?? "Unknown";

    /// <summary>
    /// Kullanıcının adı veya e-postası
    /// </summary>
    public string? UserName => CurrentUser?.Name ?? CurrentUser?.Email;

    /// <summary>
    /// Oturumun aktif olup olmadığı
    /// </summary>
    public bool IsAuthenticated => CurrentUser is not null;

    /// <summary>
    /// Bileşen ilk başlatıldığında kimlik doğrulama durumunu denetler; doğrulanmamışsa giriş sayfasına yönlendirir.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        if (AuthStateTask is not null)
        {
            var authState = await AuthStateTask;
            if (authState.User.Identity?.IsAuthenticated != true)
            {
                RedirectToLogin();
            }
        }
        else if (!IsAuthenticated)
        {
            await StateProvider.GetAuthenticationStateAsync();
            if (!IsAuthenticated)
            {
                RedirectToLogin();
            }
        }
    }

    /// <summary>
    /// Kullanıcıyı mevcut sayfa adresini returnUrl parametresi olarak ekleyerek /giris sayfasına yönlendirir.
    /// </summary>
    private void RedirectToLogin()
    {
        var currentUri = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        var returnUrl = string.IsNullOrEmpty(currentUri) || currentUri.Equals("giris", StringComparison.OrdinalIgnoreCase)
            ? "/giris"
            : $"/giris?returnUrl={Uri.EscapeDataString(currentUri)}";

        NavigationManager.NavigateTo(returnUrl, replace: true);
    }
}

