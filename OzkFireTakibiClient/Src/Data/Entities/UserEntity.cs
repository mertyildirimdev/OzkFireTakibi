namespace OzkFireTakibiClient.Src.Data.Entities;

public class UserEntity : SoftDeleteEntity<int>
{
    public string? Name { get; set; }
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string? StoreName { get; set; }

    public string? Role { get; set; }
}

public enum UserRole
{
    Admin,
    Moderator,
    Observer,
    User
}

public static class UserRoleHelper
{
    public static UserRole FromString(string role)
    {
        return role.ToLowerInvariant() switch
        {
            "admin" => UserRole.Admin,
            "moderator" => UserRole.Moderator,
            "observer" => UserRole.Observer,
            "user" => UserRole.User,
            _ => throw new ArgumentException($"Invalid role: {role}")
        };
    }

    public static string ToString(UserRole role)
    {
        return role switch
        {
            UserRole.Admin => "Admin",
            UserRole.Moderator => "Moderator",
            UserRole.Observer => "Observer",
            UserRole.User => "User",
            _ => throw new ArgumentException($"Invalid role: {role}")
        };
    }
}

public class AuthSession
{
    public int UserId { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
