namespace OzkFireTakibiClient.Src;

using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.Extensions.DependencyInjection;
using OzkFireTakibiClient.Data.Entities;
using OzkFireTakibiClient.Src.Services;

public class CustomStateProvider(ProtectedLocalStorage protectedLocalStorage, IServiceProvider serviceProvider) : AuthenticationStateProvider
{
    private const string StorageKey = "auth_session";
    private readonly ProtectedLocalStorage _protectedLocalStorage = protectedLocalStorage;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private UserEntity? _currentUser;
    private bool _hasAttemptedRestore;

    public UserEntity? CurrentUser => _currentUser;

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

    public async Task MarkUserAsAuthenticatedAsync(UserEntity user, bool rememberMe)
    {
        _currentUser = user;
        _hasAttemptedRestore = true;

        if (rememberMe)
        {
            try
            {
                var session = new AuthSession
                {
                    UserId = user.Id,
                    ExpiresAtUtc = DateTime.UtcNow.AddDays(30)
                };
                await _protectedLocalStorage.SetAsync(StorageKey, session);
            }
            catch
            {
                // Tarayıcı depolama hatası oluşursa oturum yine de bellekte açılmış olur
            }
        }
        else
        {
            try
            {
                await _protectedLocalStorage.DeleteAsync(StorageKey);
            }
            catch { }
        }

        var identity = CreateIdentity(user);
        var principal = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
    }

    public async Task MarkUserAsLoggedOutAsync()
    {
        _currentUser = null;
        _hasAttemptedRestore = true;

        try
        {
            await _protectedLocalStorage.DeleteAsync(StorageKey);
        }
        catch { }

        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymousUser)));
    }

    private async Task TryRestoreSessionAsync()
    {
        try
        {
            var result = await _protectedLocalStorage.GetAsync<AuthSession>(StorageKey);
            if (result.Success && result.Value is not null)
            {
                var session = result.Value;
                if (session.ExpiresAtUtc > DateTime.UtcNow)
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

                await _protectedLocalStorage.DeleteAsync(StorageKey);
            }

            _hasAttemptedRestore = true;
        }
        catch (InvalidOperationException)
        {
            // Prerendering aşamasında JSInterop çağrılamaz.
        }
        catch
        {
            _hasAttemptedRestore = true;
            try
            {
                await _protectedLocalStorage.DeleteAsync(StorageKey);
            }
            catch { }
        }
    }

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

        return new ClaimsIdentity(claims, "CustomAuth");
    }
}
