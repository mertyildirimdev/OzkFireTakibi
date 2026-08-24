namespace OzkFireTakibiClient.Src.Data.Entities;

/// <summary>
/// Sistem kullanıcılarını temsil eden veritabanı varlığı.
/// </summary>
public class UserEntity : SoftDeleteEntity<int>
{
    /// <summary>
    /// Kullanıcının adı soyadı
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Giriş için kullanılan benzersiz e-posta adresi
    /// </summary>
    public string Email { get; set; } = default!;

    /// <summary>
    /// Kullanıcı şifresi (düz metin / hash)
    /// </summary>
    public string Password { get; set; } = default!;

    /// <summary>
    /// Kullanıcının yetkili/bağlı olduğu mağaza veya şube adı (opsiyonel)
    /// </summary>
    public string? StoreName { get; set; }

    /// <summary>
    /// Kullanıcının bağlı olduğu mağazanın Excel'deki sabit Depo No değeri.
    /// </summary>
    public int? StoreNumber { get; set; }

    /// <summary>
    /// Kullanıcının rolü (Admin, Moderator, Observer, User)
    /// </summary>
    public string? Role { get; set; }
}

/// <summary>
/// Uygulama kullanıcı rolleri.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Tüm sisteme, rapor yükleme/silme ve kullanıcı yönetimine tam erişim
    /// </summary>
    Admin,

    /// <summary>
    /// Rapor yükleme ve görüntüleme yetkisine sahip kullanıcı
    /// </summary>
    Moderator,

    /// <summary>
    /// Yalnızca raporları ve analizleri izleme yetkisine sahip gözlemci
    /// </summary>
    Observer,

    /// <summary>
    /// Standart kullanıcı
    /// </summary>
    User
}

/// <summary>
/// Kullanıcı rolü dönüştürme ve doğrulama yardımcı sınıfı.
/// </summary>
public static class UserRoleHelper
{
    /// <summary>
    /// Metinsel rol adını UserRole enum değerine dönüştürür.
    /// </summary>
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

    /// <summary>
    /// UserRole enum değerini metin karşılığına dönüştürür.
    /// </summary>
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

