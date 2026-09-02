using OzkFireTakibi.Dashboard.Authentication;

namespace OzkFireTakibi.Dashboard.Services;

public sealed class LoginService(UserService userService, DashboardAuthenticationStateProvider stateProvider)
{
    public async Task<bool> LoginAsync(string email, string password, bool rememberMe)
    {
        var user = await userService.LoginAsync(email, password);
        if (user is null) return false;

        await stateProvider.SignInAsync(user, rememberMe);
        return true;
    }

    public Task LogoutAsync() => stateProvider.SignOutAsync();
}
