using Microsoft.EntityFrameworkCore;

namespace OzkFireTakibi.Dashboard.Data;

public sealed class ReportDbContext(DbContextOptions<ReportDbContext> options) : DbContext(options)
{
    public DbSet<UserRecord> Users => Set<UserRecord>();
    public DbSet<ReportPeriodRecord> ReportPeriods => Set<ReportPeriodRecord>();
    public DbSet<ReportImportRecord> ReportImports => Set<ReportImportRecord>();
    public DbSet<ReportRowRecord> ReportRows => Set<ReportRowRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserRecord>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(row => row.Id);
            entity.Property(row => row.Email).IsRequired();
            entity.Property(row => row.Password).IsRequired();
        });

        modelBuilder.Entity<ReportPeriodRecord>(entity =>
        {
            entity.ToTable("report_periods");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CategorySignature).HasMaxLength(64);
        });

        modelBuilder.Entity<ReportImportRecord>(entity =>
        {
            entity.ToTable("report_imports");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PeriodType).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.OriginalFileName).HasMaxLength(260);
        });

        modelBuilder.Entity<ReportRowRecord>(entity =>
        {
            entity.ToTable("report_rows");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RowType).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.SourceReportId).HasColumnName("rpr_id");
            entity.Property(x => x.SourceReportType).HasColumnName("rpr_tip");
            entity.Property(x => x.StoreNumber).HasColumnName("Depo No");
            entity.Property(x => x.StoreName).HasColumnName("Depo Adı");
            entity.Property(x => x.CategoryCode).HasColumnName("Kategori Kodu");
            entity.Property(x => x.CategoryName).HasColumnName("Kategori İsmi");
            entity.Property(x => x.StockCode).HasColumnName("Stok Kodu");
            entity.Property(x => x.StockName).HasColumnName("Stok İsmi");
            entity.Property(x => x.AlternativeName).HasColumnName("Alternatif İsim");
            entity.Property(x => x.CostGroupType).HasColumnName("Maliyet Grup Tipi");
            entity.Property(x => x.CostGroupCode).HasColumnName("Maliyet Grup Kodu");
            entity.Property(x => x.PurchaseGroupValueFactor).HasColumnName("Satın Alma Grubu Değer Çarpanı");
            entity.Property(x => x.PurchaseStockValueFactor).HasColumnName("Satın Alma Stok Değer Çarpanı");
            entity.Property(x => x.OpeningQuantity).HasColumnName("Dönem Başı Miktar");
            entity.Property(x => x.OpeningAmount).HasColumnName("Dönem Başı Tutar");
            entity.Property(x => x.CompanyPurchaseQuantity).HasColumnName("Firma Alış Miktar");
            entity.Property(x => x.CompanyPurchaseAmount).HasColumnName("Firma Alış Tutar");
            entity.Property(x => x.WarehouseTransferInQuantity).HasColumnName("Depo Sevk Alış Miktar");
            entity.Property(x => x.WarehouseTransferInAmount).HasColumnName("Depo Sevk Alış Tutar");
            entity.Property(x => x.WarehouseTransferOutQuantity).HasColumnName("Depo Sevk Satış Miktar");
            entity.Property(x => x.WarehouseTransferOutAmount).HasColumnName("Depo Sevk Satış Tutar");
            entity.Property(x => x.StoreSalesQuantity).HasColumnName("Mağaza Satış Miktar");
            entity.Property(x => x.StoreSalesAmount).HasColumnName("Mağaza Satış Tutar");
            entity.Property(x => x.CostOfSales).HasColumnName("Satış Maliyeti");
            entity.Property(x => x.WasteRate).HasColumnName("Fire Oranı");
            entity.Property(x => x.WasteQuantity).HasColumnName("Fire Miktarı");
            entity.Property(x => x.WasteAmount).HasColumnName("Fire Tutarı");
            entity.Property(x => x.ClosingQuantity).HasColumnName("Dönem Sonu Miktar");
            entity.Property(x => x.ClosingAmount).HasColumnName("Dönem Sonu Tutar");
            entity.Property(x => x.ProfitAmount).HasColumnName("Kar Tutar");
            entity.Property(x => x.ProfitRate).HasColumnName("Kar Oran");
            entity.Property(x => x.CategoryProfitRate).HasColumnName("Kategori Kar Oran");
            entity.Property(x => x.CategoryWasteRate).HasColumnName("Kategori Fire Oran");

            entity.Property(x => x.PurchaseGroupValueFactor).HasPrecision(20, 6);
            entity.Property(x => x.PurchaseStockValueFactor).HasPrecision(20, 6);
            entity.Property(x => x.OpeningQuantity).HasPrecision(20, 6);
            entity.Property(x => x.OpeningAmount).HasPrecision(20, 6);
            entity.Property(x => x.CompanyPurchaseQuantity).HasPrecision(20, 6);
            entity.Property(x => x.CompanyPurchaseAmount).HasPrecision(20, 6);
            entity.Property(x => x.WarehouseTransferInQuantity).HasPrecision(20, 6);
            entity.Property(x => x.WarehouseTransferInAmount).HasPrecision(20, 6);
            entity.Property(x => x.WarehouseTransferOutQuantity).HasPrecision(20, 6);
            entity.Property(x => x.WarehouseTransferOutAmount).HasPrecision(20, 6);
            entity.Property(x => x.StoreSalesQuantity).HasPrecision(20, 6);
            entity.Property(x => x.StoreSalesAmount).HasPrecision(20, 6);
            entity.Property(x => x.CostOfSales).HasPrecision(20, 6);
            entity.Property(x => x.WasteRate).HasPrecision(20, 6);
            entity.Property(x => x.WasteQuantity).HasPrecision(20, 6);
            entity.Property(x => x.WasteAmount).HasPrecision(20, 6);
            entity.Property(x => x.ClosingQuantity).HasPrecision(20, 6);
            entity.Property(x => x.ClosingAmount).HasPrecision(20, 6);
            entity.Property(x => x.ProfitAmount).HasPrecision(20, 6);
            entity.Property(x => x.ProfitRate).HasPrecision(20, 6);
            entity.Property(x => x.CategoryProfitRate).HasPrecision(20, 6);
            entity.Property(x => x.CategoryWasteRate).HasPrecision(20, 6);
        });
    }
}

public sealed class UserRecord
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? StoreName { get; set; }
    public int? StoreNumber { get; set; }
    public string? Role { get; set; }
    public bool IsDeleted { get; set; }
}

public sealed class ReportPeriodRecord
{
    public long Id { get; set; }
    public string CategorySignature { get; set; } = string.Empty;
    public DateOnly EndDate { get; set; }
}

public sealed class ReportImportRecord
{
    public long Id { get; set; }
    public long ReportPeriodId { get; set; }
    public ReportPeriodType PeriodType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int TotalRowCount { get; set; }
}

public sealed class ReportRowRecord
{
    public long Id { get; set; }
    public long ReportImportId { get; set; }
    public int SourceRowNumber { get; set; }
    public ReportRowType RowType { get; set; }
    public int SourceReportId { get; set; }
    public string SourceReportType { get; set; } = string.Empty;
    public int? StoreNumber { get; set; }
    public string? StoreName { get; set; }
    public string? CategoryCode { get; set; }
    public string? CategoryName { get; set; }
    public string? StockCode { get; set; }
    public string? StockName { get; set; }
    public string? AlternativeName { get; set; }
    public string? CostGroupType { get; set; }
    public string? CostGroupCode { get; set; }
    public decimal? PurchaseGroupValueFactor { get; set; }
    public decimal? PurchaseStockValueFactor { get; set; }
    public decimal? OpeningQuantity { get; set; }
    public decimal? OpeningAmount { get; set; }
    public decimal? CompanyPurchaseQuantity { get; set; }
    public decimal? CompanyPurchaseAmount { get; set; }
    public decimal? WarehouseTransferInQuantity { get; set; }
    public decimal? WarehouseTransferInAmount { get; set; }
    public decimal? WarehouseTransferOutQuantity { get; set; }
    public decimal? WarehouseTransferOutAmount { get; set; }
    public decimal? StoreSalesQuantity { get; set; }
    public decimal? StoreSalesAmount { get; set; }
    public decimal? CostOfSales { get; set; }
    public decimal? WasteRate { get; set; }
    public decimal? WasteQuantity { get; set; }
    public decimal? WasteAmount { get; set; }
    public decimal? ClosingQuantity { get; set; }
    public decimal? ClosingAmount { get; set; }
    public decimal? ProfitAmount { get; set; }
    public decimal? ProfitRate { get; set; }
    public decimal? CategoryProfitRate { get; set; }
    public decimal? CategoryWasteRate { get; set; }
}

public enum ReportPeriodType { Monthly, Cumulative }

public enum ReportRowType
{
    General,
    CategorySummary,
    StoreSummary,
    StoreCategory,
    ProductSummary,
    StoreProduct
}
