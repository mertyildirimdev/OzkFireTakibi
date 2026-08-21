using Microsoft.EntityFrameworkCore;
using OzkFireTakibiClient.Src.Data.Entities;

namespace OzkFireTakibiClient.Src.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users { get; set; } = default!;
    public DbSet<ReportPeriodEntity> ReportPeriods { get; set; } = default!;
    public DbSet<ReportImportEntity> ReportImports { get; set; } = default!;
    public DbSet<ReportRowEntity> ReportRows { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureReportImports(modelBuilder);

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

    private static void ConfigureReportImports(ModelBuilder modelBuilder)
    {
        var reportPeriod = modelBuilder.Entity<ReportPeriodEntity>();
        reportPeriod.ToTable("report_periods");
        reportPeriod.Property(x => x.Scope).HasConversion<string>().HasMaxLength(40);
        reportPeriod.HasIndex(x => new { x.Scope, x.EndDate }).IsUnique();

        var reportImport = modelBuilder.Entity<ReportImportEntity>();
        reportImport.ToTable("report_imports");
        reportImport.Property(x => x.Scope).HasConversion<string>().HasMaxLength(40);
        reportImport.Property(x => x.PeriodType).HasConversion<string>().HasMaxLength(20);
        reportImport.Property(x => x.OriginalFileName).HasMaxLength(260);
        reportImport.Property(x => x.FileHash).HasMaxLength(64);
        reportImport.HasIndex(x => x.FileHash).IsUnique();
        reportImport
            .HasIndex(x => new { x.ReportPeriodId, x.PeriodType, x.IsActive })
            .IsUnique()
            .HasFilter("\"IsActive\" = 1");
        reportImport
            .HasOne(x => x.ReportPeriod)
            .WithMany(x => x.Imports)
            .HasForeignKey(x => x.ReportPeriodId)
            .OnDelete(DeleteBehavior.Restrict);
        reportImport
            .HasOne(x => x.UploadedByUser)
            .WithMany()
            .HasForeignKey(x => x.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        var reportRow = modelBuilder.Entity<ReportRowEntity>();
        reportRow.ToTable("report_rows");
        reportRow.Property(x => x.RowType).HasConversion<string>().HasMaxLength(30);
        reportRow.Property(x => x.SourceReportId).HasColumnName("rpr_id");
        reportRow.Property(x => x.SourceReportType).HasColumnName("rpr_tip").HasMaxLength(120);
        reportRow.Property(x => x.StoreNumber).HasColumnName("Depo No");
        reportRow.Property(x => x.StoreName).HasColumnName("Depo Adı").HasMaxLength(160);
        reportRow.Property(x => x.CategoryCode).HasColumnName("Kategori Kodu").HasMaxLength(40);
        reportRow.Property(x => x.CategoryName).HasColumnName("Kategori İsmi").HasMaxLength(200);
        reportRow.Property(x => x.StockCode).HasColumnName("Stok Kodu").HasMaxLength(60);
        reportRow.Property(x => x.StockName).HasColumnName("Stok İsmi").HasMaxLength(240);
        reportRow.Property(x => x.AlternativeName).HasColumnName("Alternatif İsim").HasMaxLength(240);
        reportRow.Property(x => x.CostGroupType).HasColumnName("Maliyet Grup Tipi").HasMaxLength(100);
        reportRow.Property(x => x.CostGroupCode).HasColumnName("Maliyet Grup Kodu").HasMaxLength(100);
        reportRow.HasIndex(x => new { x.ReportImportId, x.SourceRowNumber }).IsUnique();
        reportRow.HasIndex(x => new { x.ReportImportId, x.RowType });
        reportRow.HasIndex(x => new { x.ReportImportId, x.StoreNumber });
        reportRow.HasIndex(x => new { x.ReportImportId, x.CategoryCode });
        reportRow.HasIndex(x => new { x.ReportImportId, x.StockCode });
        reportRow
            .HasOne(x => x.ReportImport)
            .WithMany(x => x.Rows)
            .HasForeignKey(x => x.ReportImportId)
            .OnDelete(DeleteBehavior.Cascade);

        reportRow.Property(x => x.PurchaseGroupValueFactor).HasColumnName("Satın Alma Grubu Değer Çarpanı").HasConversion<double>();
        reportRow.Property(x => x.PurchaseStockValueFactor).HasColumnName("Satın Alma Stok Değer Çarpanı").HasConversion<double>();
        reportRow.Property(x => x.OpeningQuantity).HasColumnName("Dönem Başı Miktar").HasConversion<double>();
        reportRow.Property(x => x.OpeningAmount).HasColumnName("Dönem Başı Tutar").HasConversion<double>();
        reportRow.Property(x => x.CompanyPurchaseQuantity).HasColumnName("Firma Alış Miktar").HasConversion<double>();
        reportRow.Property(x => x.CompanyPurchaseAmount).HasColumnName("Firma Alış Tutar").HasConversion<double>();
        reportRow.Property(x => x.WarehouseTransferInQuantity).HasColumnName("Depo Sevk Alış Miktar").HasConversion<double>();
        reportRow.Property(x => x.WarehouseTransferInAmount).HasColumnName("Depo Sevk Alış Tutar").HasConversion<double>();
        reportRow.Property(x => x.WarehouseTransferOutQuantity).HasColumnName("Depo Sevk Satış Miktar").HasConversion<double>();
        reportRow.Property(x => x.WarehouseTransferOutAmount).HasColumnName("Depo Sevk Satış Tutar").HasConversion<double>();
        reportRow.Property(x => x.StoreSalesQuantity).HasColumnName("Mağaza Satış Miktar").HasConversion<double>();
        reportRow.Property(x => x.StoreSalesAmount).HasColumnName("Mağaza Satış Tutar").HasConversion<double>();
        reportRow.Property(x => x.CostOfSales).HasColumnName("Satış Maliyeti").HasConversion<double>();
        reportRow.Property(x => x.WasteRate).HasColumnName("Fire Oranı").HasConversion<double>();
        reportRow.Property(x => x.WasteQuantity).HasColumnName("Fire Miktarı").HasConversion<double>();
        reportRow.Property(x => x.WasteAmount).HasColumnName("Fire Tutarı").HasConversion<double>();
        reportRow.Property(x => x.ClosingQuantity).HasColumnName("Dönem Sonu Miktar").HasConversion<double>();
        reportRow.Property(x => x.ClosingAmount).HasColumnName("Dönem Sonu Tutar").HasConversion<double>();
        reportRow.Property(x => x.ProfitAmount).HasColumnName("Kar Tutar").HasConversion<double>();
        reportRow.Property(x => x.ProfitRate).HasColumnName("Kar Oran").HasConversion<double>();
        reportRow.Property(x => x.CategoryProfitRate).HasColumnName("Kategori Kar Oran").HasConversion<double>();
        reportRow.Property(x => x.CategoryWasteRate).HasColumnName("Kategori Fire Oran").HasConversion<double>();
    }
}
