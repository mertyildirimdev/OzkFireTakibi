using Microsoft.EntityFrameworkCore;
using OzkFireTakibiClient.Data.Entities;

namespace OzkFireTakibiClient.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Mevcut migration ve SQLite şemasıyla uyumlu olarak tablo adlarını küçük harfle tut.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var currentTableName = entityType.GetTableName();
            if (!string.IsNullOrEmpty(currentTableName))
            {
                entityType.SetTableName(currentTableName.ToLowerInvariant());
            }
        }

        modelBuilder.Entity<UserEntity>().HasData(
            new UserEntity
            {
                Id = 1,
                Name = "System Admin",
                Email = "admin@ozkfiretakibi.local",
                Password = "admin123",
                Role = UserRole.Admin.ToString(),
                CreatedAt = new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc),
                IsDeleted = false
            },
            new UserEntity
            {
                Id = 2,
                Name = "Normal User",
                Email = "user@ozkfiretakibi.local",
                Password = "user123",
                Role = UserRole.User.ToString(),
                CreatedAt = new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc),
                IsDeleted = false
            });
    }
}
