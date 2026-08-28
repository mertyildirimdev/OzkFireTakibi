namespace OzkFireTakibiClient.Src.Authorization;

using System;

/// <summary>
/// Tarayıcı sessionStorage veya localStorage alanında korumalı olarak tutulan oturum verisi.
/// </summary>
public class AuthSession
{
    /// <summary>
    /// Oturum sahibi kullanıcının kimliği
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Oturumun geçerlilik bitiş tarihi (UTC)
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }
}
