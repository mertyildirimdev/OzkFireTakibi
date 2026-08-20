using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using OzkFireTakibiClient.Data.Entities;

namespace OzkFireTakibiClient.Src.Components.Controls;

public class AuthRequiredComponent : ComponentBase
{
    [CascadingParameter]
    private Task<AuthenticationState>? AuthStateTask { get; set; }

    [Inject]
    public CustomStateProvider StateProvider { get; set; } = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    public UserEntity? CurrentUser => StateProvider.CurrentUser;
    public string UserRole => CurrentUser?.Role ?? "Unknown";
    public string? UserName => CurrentUser?.Name ?? CurrentUser?.Email;
    public bool IsAuthenticated => CurrentUser is not null;

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

    private void RedirectToLogin()
    {
        var currentUri = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        var returnUrl = string.IsNullOrEmpty(currentUri) || currentUri.Equals("giris", StringComparison.OrdinalIgnoreCase)
            ? "/giris"
            : $"/giris?returnUrl={Uri.EscapeDataString(currentUri)}";

        NavigationManager.NavigateTo(returnUrl, replace: true);
    }
}
