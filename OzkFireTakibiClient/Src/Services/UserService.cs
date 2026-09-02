using OzkFireTakibiClient.Src.Data;
using Microsoft.EntityFrameworkCore;
using OzkFireTakibiClient.Src.Data.Entities;

namespace OzkFireTakibiClient.Src.Services;

/// <summary>
/// Kullanıcı yönetimi, kimlik doğrulama kontrolleri ve CRUD işlemlerini yürüten servis.
/// </summary>
public class UserService(IDbContextFactory<AppDbContext> dbContextFactory)
{
    /// <summary>
    /// Verilen kullanıcı kimliğine (ID) ait rol adını döndürür.
    /// </summary>
    public virtual async Task<string> GetUserRoleAsync(int userId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

        return user?.Role ?? "Unknown";
    }

    /// <summary>
    /// Kimlik (ID) ile silinmemiş kullanıcı kaydını getirir.
    /// </summary>
    public virtual async Task<UserEntity?> GetUserByIdAsync(int id)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
    }

    /// <summary>
    /// E-posta adresi ile silinmemiş kullanıcı kaydını getirir.
    /// </summary>
    public virtual async Task<UserEntity?> GetUserByEmailAsync(string email)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var normalizedEmail = email.Trim();
        return await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail && !u.IsDeleted);
    }

    /// <summary>
    /// E-posta ve şifre eşleşmesini kontrol ederek kullanıcı doğrulaması yapar.
    /// </summary>
    public virtual async Task<UserEntity?> LoginAsync(string email, string password)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var normalizedEmail = email.Trim();
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Email == normalizedEmail &&
                !u.IsDeleted);

        // BCrypt kullanımı devre dışı bırakıldı; parolalar veritabanında doğrudan saklanıyor.
        // if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
        if (user is null || user.Password != password)
        {
            return null;
        }

        return user;
    }

    /// <summary>
    /// Sistemdeki tüm aktif (silinmemiş) kullanıcıları listeler.
    /// </summary>
    public virtual async Task<List<UserEntity>> GetAllUsersAsync()
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted)
            .ToListAsync();
    }

    /// <summary>
    /// Yeni bir kullanıcı kaydı oluşturur.
    /// </summary>
    public virtual async Task<bool> CreateUserAsync(UserEntity user)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        // BCrypt kullanımı devre dışı bırakıldı; girilen parola doğrudan kaydediliyor.
        // user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
        user.CreatedAt = now;
        user.UpdatedAt = now;
        user.IsDeleted = false;

        await dbContext.Users.AddAsync(user);
        return await dbContext.SaveChangesAsync() > 0;
    }

    /// <summary>
    /// Belirtilen kullanıcıyı mantıksal olarak siler (Soft Delete - IsDeleted = true).
    /// </summary>
    public virtual async Task<bool> DeleteUserAsync(int id)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

        if (user is null)
        {
            return false;
        }

        user.IsDeleted = true;
        user.UpdatedAt = DateTime.UtcNow;
        return await dbContext.SaveChangesAsync() > 0;
    }
}

