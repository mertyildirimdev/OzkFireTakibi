namespace OzkFireTakibiClient.Src;

using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using OzkFireTakibiClient.Data.Entities;

public class CustomStateProvider : AuthenticationStateProvider
{
    private UserEntity? _currentUser;

    public UserEntity? CurrentUser => _currentUser;

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var identity = _currentUser != null
            ? CreateIdentity(_currentUser)
            : new ClaimsIdentity();

        var user = new ClaimsPrincipal(identity);
        return Task.FromResult(new AuthenticationState(user));
    }

    public void MarkUserAsAuthenticated(UserEntity user)
    {
        _currentUser = user;
        var identity = CreateIdentity(user);
        var principal = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
    }

    public void MarkUserAsLoggedOut()
    {
        _currentUser = null;
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymousUser)));
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
