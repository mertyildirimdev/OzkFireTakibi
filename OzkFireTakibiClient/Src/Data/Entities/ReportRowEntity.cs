namespace OzkFireTakibiClient.Src.Data.Entities;

/// <summary>
/// Excel raporundan ayrıştırılan tekil satır verisini ve hesaplanmış metrikleri temsil eder.
/// </summary>
public class ReportRowEntity : BaseEntity<long>
{
    /// <summary>
    /// Bağlı olduğu rapor içe aktarım (ReportImport) kimliği
    /// </summary>
    public long ReportImportId { get; set; }

    /// <summary>
    /// Excel dosyasındaki orijinal satır numarası (1-indexed)
    /// </summary>
    public int SourceRowNumber { get; set; }

    /// <summary>
    /// Satırın özet/detay hiyerarşi türü
    /// </summary>
    public ReportRowType RowType { get; set; }

    /// <summary>
    /// Excel'deki rpr_id değeri (1: Genel, 2: Kategori, 3: Şubeler Genel, 4: Şubeler Grup, 5: Stoklar, 7: Şubeler Stok)
    /// </summary>
    public int SourceReportId { get; set; }

    /// <summary>
    /// Excel'deki rpr_tip metni (örn: "Genel Durum 01.08.2026-31.08.2026", "Şubeler Genel Durum" vb.)
    /// </summary>
    public string SourceReportType { get; set; } = default!;

    /// <summary>
    /// Depo / Mağaza numarası
    /// </summary>
    public int? StoreNumber { get; set; }

    /// <summary>
    /// Depo / Mağaza adı
    /// </summary>
    public string? StoreName { get; set; }

    /// <summary>
    /// Kategori kodu (örn: "12.01")
    /// </summary>
    public string? CategoryCode { get; set; }

    /// <summary>
    /// Kategori ismi (örn: "Peynir Çeşitleri")
    /// </summary>
    public string? CategoryName { get; set; }

    /// <summary>
    /// Stok / Ürün kodu
    /// </summary>
    public string? StockCode { get; set; }

    /// <summary>
    /// Stok / Ürün ismi
    /// </summary>
    public string? StockName { get; set; }

    /// <summary>
    /// Alternatif ürün ismi
    /// </summary>
    public string? AlternativeName { get; set; }

    /// <summary>
    /// Maliyet grup tipi
    /// </summary>
    public string? CostGroupType { get; set; }

    /// <summary>
    /// Maliyet grup kodu
    /// </summary>
    public string? CostGroupCode { get; set; }

    /// <summary>
    /// Satın alma grubu değer çarpanı
    /// </summary>
    public decimal? PurchaseGroupValueFactor { get; set; }

    /// <summary>
    /// Satın alma stok değer çarpanı
    /// </summary>
    public decimal? PurchaseStockValueFactor { get; set; }

    /// <summary>
    /// Dönem başı stok miktarı
    /// </summary>
    public decimal? OpeningQuantity { get; set; }

    /// <summary>
    /// Dönem başı stok parasal tutarı
    /// </summary>
    public decimal? OpeningAmount { get; set; }

    /// <summary>
    /// Tedarikçi/firma alış miktarı
    /// </summary>
    public decimal? CompanyPurchaseQuantity { get; set; }

    /// <summary>
    /// Tedarikçi/firma alış tutarı
    /// </summary>
    public decimal? CompanyPurchaseAmount { get; set; }

    /// <summary>
    /// Depo sevk alış miktarı (Merkez depodan mağazaya giriş)
    /// </summary>
    public decimal? WarehouseTransferInQuantity { get; set; }

    /// <summary>
    /// Depo sevk alış tutarı
    /// </summary>
    public decimal? WarehouseTransferInAmount { get; set; }

    /// <summary>
    /// Depo sevk satış miktarı (Mağazadan depoya/diğer şubeye çıkış)
    /// </summary>
    public decimal? WarehouseTransferOutQuantity { get; set; }

    /// <summary>
    /// Depo sevk satış tutarı
    /// </summary>
    public decimal? WarehouseTransferOutAmount { get; set; }

    /// <summary>
    /// Mağaza satış miktarı
    /// </summary>
    public decimal? StoreSalesQuantity { get; set; }

    /// <summary>
    /// Mağaza satış cirosu/tutarı
    /// </summary>
    public decimal? StoreSalesAmount { get; set; }

    /// <summary>
    /// Satılan malın maliyeti (SMM)
    /// </summary>
    public decimal? CostOfSales { get; set; }

    /// <summary>
    /// Fire yüzdesi / oranı (%)
    /// </summary>
    public decimal? WasteRate { get; set; }

    /// <summary>
    /// Fire miktarı (adet/kg vb.)
    /// </summary>
    public decimal? WasteQuantity { get; set; }

    /// <summary>
    /// Fire maliyet tutarı (TL)
    /// </summary>
    public decimal? WasteAmount { get; set; }

    /// <summary>
    /// Dönem sonu kalan stok miktarı
    /// </summary>
    public decimal? ClosingQuantity { get; set; }

    /// <summary>
    /// Dönem sonu kalan stok tutarı
    /// </summary>
    public decimal? ClosingAmount { get; set; }

    /// <summary>
    /// Brüt kâr tutarı (TL)
    /// </summary>
    public decimal? ProfitAmount { get; set; }

    /// <summary>
    /// Brüt kâr oranı (%)
    /// </summary>
    public decimal? ProfitRate { get; set; }

    /// <summary>
    /// Kategori geneli kâr oranı (%)
    /// </summary>
    public decimal? CategoryProfitRate { get; set; }

    /// <summary>
    /// Kategori geneli fire oranı (%)
    /// </summary>
    public decimal? CategoryWasteRate { get; set; }

    /// <summary>
    /// Bağlı olduğu rapor içe aktarım varlığı navigasyonu
    /// </summary>
    public ReportImportEntity ReportImport { get; set; } = default!;
}

/// <summary>
/// Rapor satırının hiyerarşi ve özet seviyesi.
/// </summary>
public enum ReportRowType
{
    /// <summary>
    /// Raporun tamamına ait tekil genel özet satırı (rpr_id = 1)
    /// </summary>
    General,

    /// <summary>
    /// Kategori bazında özet satırları (rpr_id = 2)
    /// </summary>
    CategorySummary,

    /// <summary>
    /// Mağaza/Şube bazında özet satırları (rpr_id = 3)
    /// </summary>
    StoreSummary,

    /// <summary>
    /// Mağaza ve Kategori kırılımında detay satırları (rpr_id = 4)
    /// </summary>
    StoreCategory,

    /// <summary>
    /// Ürün bazında özet satırları (rpr_id = 5)
    /// </summary>
    ProductSummary,

    /// <summary>
    /// Mağaza ve Ürün kırılımında en detay satırlar (rpr_id = 7)
    /// </summary>
    StoreProduct
}

