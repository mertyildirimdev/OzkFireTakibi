namespace OzkFireTakibiClient.Src.Data.Entities;

using System;

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
        return Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsed) ? parsed : throw new ArgumentException($"Invalid role: {role}");
    }

    /// <summary>
    /// UserRole enum değerini metin karşılığına dönüştürür.
    /// </summary>
    public static string ToString(UserRole role)
    {
        return role.ToString();
    }
}
