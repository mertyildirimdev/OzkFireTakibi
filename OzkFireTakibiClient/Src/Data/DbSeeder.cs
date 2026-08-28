using Microsoft.EntityFrameworkCore;
using OzkFireTakibiClient.Src.Data.Entities;

namespace OzkFireTakibiClient.Src.Data;

/// <summary>
/// Veritabanı başlangıç verilerini (seed) oluşturur.
/// </summary>
public static class DbSeeder
{
    /// <summary>
    /// Varsayılan kullanıcılar yoksa oluşturur.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var dbContext = await factory.CreateDbContextAsync();

        if (await dbContext.Users.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;
        dbContext.Users.AddRange(
            new UserEntity
            {
                Name = "System Admin",
                Email = "admin@ozkfiretakibi.local",
                Password = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = UserRole.Admin.ToString(),
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false
            },
            new UserEntity
            {
                Name = "Normal User",
                Email = "user@ozkfiretakibi.local",
                Password = BCrypt.Net.BCrypt.HashPassword("user123"),
                Role = UserRole.User.ToString(),
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false
            });

        await dbContext.SaveChangesAsync();
    }
}
