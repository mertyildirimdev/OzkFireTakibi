namespace OzkFireTakibi.Dashboard.Data.Entities;

/// <summary>
/// Mağaza özeti, mağaza × kategori veya mağaza × ürün satırı için açılan mazeret talebi.
/// </summary>
public class ExcuseRequestEntity : BaseEntity<long>
{
    public long ReportRowId { get; set; }
    public ExcuseSource Source { get; set; }
    public string Title { get; set; } = default!;
    public string? RequestNote { get; set; }
    public int? RequestedByUserId { get; set; }
    public decimal? ThresholdRate { get; set; }
    public ExcuseStatus Status { get; set; } = ExcuseStatus.Open;
    public ExcuseStatus? StatusBeforeSuperseded { get; set; }
    public long? SupersededByReportImportId { get; set; }
    public DateTime? RespondedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }

    public ReportRowEntity ReportRow { get; set; } = default!;
    public UserEntity? RequestedByUser { get; set; }
    public ICollection<ExcuseEntryEntity> Entries { get; set; } = new List<ExcuseEntryEntity>();
}

/// <summary>
/// Talebin eşik kuralıyla mı yoksa yetkili kullanıcı eylemiyle mi oluşturulduğunu belirtir.
/// </summary>
public enum ExcuseSource
{
    Automatic,
    Manual
}

/// <summary>
/// Mazeret talebinin mağaza yanıtı ve merkez değerlendirmesi arasındaki iş akışı durumudur.
/// </summary>
public enum ExcuseStatus
{
    /// <summary>Mağazadan ilk yanıt bekleniyor.</summary>
    Open,
    /// <summary>Mağaza yanıtladı; merkez değerlendirmesi bekleniyor.</summary>
    Answered,
    /// <summary>Merkez ek açıklama istedi; mağaza yeniden yanıtlayabilir.</summary>
    RevisionRequested,
    /// <summary>Merkez mazereti uygun buldu.</summary>
    Approved,
    /// <summary>Aynı dönem için daha yeni aylık rapor yüklendiğinden talep geçersiz kaldı.</summary>
    Superseded
}
