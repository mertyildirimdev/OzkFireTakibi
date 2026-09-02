namespace OzkFireTakibi.Dashboard.Authentication;

public sealed class AuthSession
{
    public int UserId { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
