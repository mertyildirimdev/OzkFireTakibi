using Microsoft.AspNetCore.Components;
using OzkFireTakibiClient.Data.Entities;

namespace OzkFireTakibiClient.Src.Components.Controls;

public class AuthRequiredComponent : ComponentBase
{
    [Inject]
    public CustomStateProvider StateProvider { get; set; } = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    public UserEntity? CurrentUser => StateProvider.CurrentUser;
    public string UserRole => CurrentUser?.Role ?? "Unknown";
    public string? UserName => CurrentUser?.Name ?? CurrentUser?.Email;
    public bool IsAuthenticated => CurrentUser is not null;

    protected override void OnInitialized()
    {
        if (!IsAuthenticated)
        {
            var currentUri = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
            var returnUrl = string.IsNullOrEmpty(currentUri) || currentUri.Equals("giris", StringComparison.OrdinalIgnoreCase)
                ? "/giris"
                : $"/giris?returnUrl={Uri.EscapeDataString(currentUri)}";

            NavigationManager.NavigateTo(returnUrl, replace: true);
        }
    }
}
