using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using OzkFireTakibi.Dashboard.Services;
using OzkFireTakibi.Dashboard.Data.Entities;

namespace OzkFireTakibi.Dashboard.Authentication;

public sealed class DashboardAuthenticationStateProvider(
    ProtectedLocalStorage localStorage,
    ProtectedSessionStorage sessionStorage,
    UserService userService) : AuthenticationStateProvider
{
    private const string StorageKey = "dashboard_auth_session";
    private UserEntity? _currentUser;
    private bool _restoreAttempted;

    public UserEntity? CurrentUser => _currentUser;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_restoreAttempted)
        {
            await RestoreAsync();
        }

        return CreateState(_currentUser);
    }

    public async Task SignInAsync(UserEntity user, bool rememberMe)
    {
        _currentUser = user;
        _restoreAttempted = true;
        await ClearStorageAsync();

        var session = new AuthSession
        {
            UserId = user.Id,
            ExpiresAtUtc = rememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddHours(12)
        };

        if (rememberMe)
        {
            await localStorage.SetAsync(StorageKey, session);
        }
        else
        {
            await sessionStorage.SetAsync(StorageKey, session);
        }

        NotifyAuthenticationStateChanged(Task.FromResult(CreateState(user)));
    }

    public async Task SignOutAsync()
    {
        _currentUser = null;
        _restoreAttempted = true;
        await ClearStorageAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(CreateState(null)));
    }

    private async Task RestoreAsync()
    {
        try
        {
            var sessionResult = await sessionStorage.GetAsync<AuthSession>(StorageKey);
            var session = sessionResult.Success ? sessionResult.Value : null;

            if (session is null)
            {
                var localResult = await localStorage.GetAsync<AuthSession>(StorageKey);
                session = localResult.Success ? localResult.Value : null;
            }

            if (session is not null && session.ExpiresAtUtc > DateTime.UtcNow)
            {
                _currentUser = await userService.GetByIdAsync(session.UserId);
            }

            if (_currentUser is null)
            {
                await ClearStorageAsync();
            }
        }
        catch
        {
            _currentUser = null;
            await ClearStorageAsync();
        }
        finally
        {
            _restoreAttempted = true;
        }
    }

    private async Task ClearStorageAsync()
    {
        try { await localStorage.DeleteAsync(StorageKey); } catch { }
        try { await sessionStorage.DeleteAsync(StorageKey); } catch { }
    }

    private static AuthenticationState CreateState(UserEntity? user)
    {
        if (user is null)
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name ?? user.Email),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role ?? "User")
        };

        if (!string.IsNullOrWhiteSpace(user.StoreName)) claims.Add(new Claim("StoreName", user.StoreName));
        if (user.StoreNumber.HasValue) claims.Add(new Claim("StoreNumber", user.StoreNumber.Value.ToString()));

        return new AuthenticationState(
            new ClaimsPrincipal(new ClaimsIdentity(claims, "DashboardAuthentication")));
    }
}
