namespace OzkFireTakibi.Dashboard.Data.Entities;

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


