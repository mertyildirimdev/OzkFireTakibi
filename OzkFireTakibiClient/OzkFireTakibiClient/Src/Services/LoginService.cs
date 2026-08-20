namespace OzkFireTakibiClient.Src.Services;

using OzkFireTakibiClient.Data;
using OzkFireTakibiClient.Data.Entities;

public class LoginService(AppDbContext dbContext, UserService userService, CustomStateProvider customStateProvider) : BaseService(dbContext)
{
    private readonly UserService _userService = userService;
    private readonly CustomStateProvider _customStateProvider = customStateProvider;

    public UserEntity? CurrentUser => _customStateProvider.CurrentUser;
    public bool IsAuthenticated => _customStateProvider.CurrentUser != null;

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

    public async Task LogoutAsync()
    {
        await _customStateProvider.MarkUserAsLoggedOutAsync();
    }

    public void Logout()
    {
        _ = LogoutAsync();
    }
}
