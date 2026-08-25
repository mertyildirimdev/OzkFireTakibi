using Microsoft.EntityFrameworkCore;
using OzkFireTakibiClient.Src.Data.Entities;

namespace OzkFireTakibiClient.Src.Data;

/// <summary>
/// Uygulamanın Entity Framework Core veritabanı bağlamı (DbContext).
/// SQL Server veritabanı şeması, tablo/kolon eşlemeleri, indeksler ve ilişkileri yapılandırır.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Kullanıcı tablosu
    /// </summary>
    public DbSet<UserEntity> Users { get; set; } = default!;

    /// <summary>
    /// Rapor dönemleri tablosu
    /// </summary>
    public DbSet<ReportPeriodEntity> ReportPeriods { get; set; } = default!;

    /// <summary>
    /// Yüklenen raporların metaveri tablosu
    /// </summary>
    public DbSet<ReportImportEntity> ReportImports { get; set; } = default!;

    /// <summary>
    /// Rapor satırları ve metrik verileri tablosu
    /// </summary>
    public DbSet<ReportRowEntity> ReportRows { get; set; } = default!;

    /// <summary>
    /// Excel raporlarından senkronize edilen mağazalar ve mazeret kapsamları.
    /// </summary>
    public DbSet<StoreEntity> Stores { get; set; } = default!;

    /// <summary>
    /// Otomatik veya manuel olarak açılan mazeret talepleri.
    /// </summary>
    public DbSet<ExcuseRequestEntity> ExcuseRequests { get; set; } = default!;

    /// <summary>
    /// Mazeret taleplerinin mağaza yanıtı ve değerlendirme geçmişi.
    /// </summary>
    public DbSet<ExcuseEntryEntity> ExcuseEntries { get; set; } = default!;

    /// <summary>
    /// Model ve şema yapılandırması
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Raporlama ve mazeret alanlarını ayrı metotlarda tutarak model kurulumunu okunabilir bırak.
        ConfigureReportImports(modelBuilder);
        ConfigureExcuses(modelBuilder);

        // Veritabanı tablo adlarını tutarlı biçimde küçük harfle tanımla.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var currentTableName = entityType.GetTableName();
            if (!string.IsNullOrEmpty(currentTableName))
            {
                entityType.SetTableName(currentTableName.ToLowerInvariant());
            }
        }

        // Başlangıç kullanıcılarını tohumla (Seed Data)
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
        // Kategori imzası ve dönem sonu, dosya adından bağımsız rapor çifti kimliğidir.
        var reportPeriod = modelBuilder.Entity<ReportPeriodEntity>();
        reportPeriod.ToTable("report_periods");
        reportPeriod.Property(x => x.CategorySignature).HasMaxLength(64);
        reportPeriod.HasIndex(x => new { x.CategorySignature, x.EndDate }).IsUnique();

        var reportImport = modelBuilder.Entity<ReportImportEntity>();
        reportImport.ToTable("report_imports");
        reportImport.Property(x => x.PeriodType).HasConversion<string>().HasMaxLength(20);
        reportImport.Property(x => x.OriginalFileName).HasMaxLength(260);
        reportImport.Property(x => x.FileHash).HasMaxLength(64);
        reportImport.HasIndex(x => x.FileHash).IsUnique();
        // Bir dönemde her rapor türünden yalnızca bir sürüm aktif kalabilir.
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
        reportRow.HasIndex(x => new { x.StoreNumber, x.RowType });
        reportRow.HasIndex(x => new { x.ReportImportId, x.CategoryCode });
        reportRow.HasIndex(x => new { x.ReportImportId, x.StockCode });
        reportRow
            .HasOne(x => x.ReportImport)
            .WithMany(x => x.Rows)
            .HasForeignKey(x => x.ReportImportId)
            .OnDelete(DeleteBehavior.Cascade);

        reportRow.Property(x => x.PurchaseGroupValueFactor).HasColumnName("Satın Alma Grubu Değer Çarpanı").HasPrecision(20, 6);
        reportRow.Property(x => x.PurchaseStockValueFactor).HasColumnName("Satın Alma Stok Değer Çarpanı").HasPrecision(20, 6);
        reportRow.Property(x => x.OpeningQuantity).HasColumnName("Dönem Başı Miktar").HasPrecision(20, 6);
        reportRow.Property(x => x.OpeningAmount).HasColumnName("Dönem Başı Tutar").HasPrecision(20, 6);
        reportRow.Property(x => x.CompanyPurchaseQuantity).HasColumnName("Firma Alış Miktar").HasPrecision(20, 6);
        reportRow.Property(x => x.CompanyPurchaseAmount).HasColumnName("Firma Alış Tutar").HasPrecision(20, 6);
        reportRow.Property(x => x.WarehouseTransferInQuantity).HasColumnName("Depo Sevk Alış Miktar").HasPrecision(20, 6);
        reportRow.Property(x => x.WarehouseTransferInAmount).HasColumnName("Depo Sevk Alış Tutar").HasPrecision(20, 6);
        reportRow.Property(x => x.WarehouseTransferOutQuantity).HasColumnName("Depo Sevk Satış Miktar").HasPrecision(20, 6);
        reportRow.Property(x => x.WarehouseTransferOutAmount).HasColumnName("Depo Sevk Satış Tutar").HasPrecision(20, 6);
        reportRow.Property(x => x.StoreSalesQuantity).HasColumnName("Mağaza Satış Miktar").HasPrecision(20, 6);
        reportRow.Property(x => x.StoreSalesAmount).HasColumnName("Mağaza Satış Tutar").HasPrecision(20, 6);
        reportRow.Property(x => x.CostOfSales).HasColumnName("Satış Maliyeti").HasPrecision(20, 6);
        reportRow.Property(x => x.WasteRate).HasColumnName("Fire Oranı").HasPrecision(20, 6);
        reportRow.Property(x => x.WasteQuantity).HasColumnName("Fire Miktarı").HasPrecision(20, 6);
        reportRow.Property(x => x.WasteAmount).HasColumnName("Fire Tutarı").HasPrecision(20, 6);
        reportRow.Property(x => x.ClosingQuantity).HasColumnName("Dönem Sonu Miktar").HasPrecision(20, 6);
        reportRow.Property(x => x.ClosingAmount).HasColumnName("Dönem Sonu Tutar").HasPrecision(20, 6);
        reportRow.Property(x => x.ProfitAmount).HasColumnName("Kar Tutar").HasPrecision(20, 6);
        reportRow.Property(x => x.ProfitRate).HasColumnName("Kar Oran").HasPrecision(20, 6);
        reportRow.Property(x => x.CategoryProfitRate).HasColumnName("Kategori Kar Oran").HasPrecision(20, 6);
        reportRow.Property(x => x.CategoryWasteRate).HasColumnName("Kategori Fire Oran").HasPrecision(20, 6);
    }

    private static void ConfigureExcuses(ModelBuilder modelBuilder)
    {
        // Mağaza numarası Excel'deki Depo No olduğu için veritabanı tarafından üretilmez.
        var store = modelBuilder.Entity<StoreEntity>();
        store.ToTable("stores");
        store.Property(x => x.Id).ValueGeneratedNever();
        store.Property(x => x.Name).HasMaxLength(160);

        var request = modelBuilder.Entity<ExcuseRequestEntity>();
        request.ToTable("excuse_requests");
        request.Property(x => x.Source).HasConversion<string>().HasMaxLength(20);
        request.Property(x => x.Title).HasMaxLength(300);
        request.Property(x => x.RequestNote).HasMaxLength(2000);
        request.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        request.Property(x => x.StatusBeforeSuperseded).HasConversion<string>().HasMaxLength(30);
        request.Property(x => x.ThresholdRate).HasPrecision(20, 6);
        // Aynı rapor satırı için ikinci bir otomatik veya manuel talep açılamaz.
        request.HasIndex(x => x.ReportRowId).IsUnique();
        request.HasIndex(x => new { x.Status, x.CreatedAt });
        request
            .HasOne(x => x.ReportRow)
            .WithOne(x => x.ExcuseRequest)
            .HasForeignKey<ExcuseRequestEntity>(x => x.ReportRowId)
            .OnDelete(DeleteBehavior.Cascade);
        request
            .HasOne(x => x.RequestedByUser)
            .WithMany()
            .HasForeignKey(x => x.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        var entry = modelBuilder.Entity<ExcuseEntryEntity>();
        entry.ToTable("excuse_entries");
        entry.Property(x => x.EntryType).HasConversion<string>().HasMaxLength(30);
        entry.Property(x => x.ReasonType).HasConversion<string>().HasMaxLength(40);
        entry.Property(x => x.Message).HasMaxLength(2000);
        entry.HasIndex(x => new { x.ExcuseRequestId, x.CreatedAt });
        entry
            .HasOne(x => x.ExcuseRequest)
            .WithMany(x => x.Entries)
            .HasForeignKey(x => x.ExcuseRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        entry
            .HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserEntity>().HasIndex(x => x.StoreNumber);
    }
}
