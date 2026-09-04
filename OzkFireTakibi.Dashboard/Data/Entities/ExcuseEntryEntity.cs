namespace OzkFireTakibi.Dashboard.Data.Entities;

/// <summary>
/// Bir mazeret talebine eklenen mağaza cevabı veya yönetici değerlendirmesi.
/// </summary>
public class ExcuseEntryEntity : BaseEntity<long>
{
    public long ExcuseRequestId { get; set; }
    public int CreatedByUserId { get; set; }
    public ExcuseEntryType EntryType { get; set; }
    public ExcuseReasonType? ReasonType { get; set; }
    public string Message { get; set; } = default!;

    public ExcuseRequestEntity ExcuseRequest { get; set; } = default!;
    public UserEntity CreatedByUser { get; set; } = default!;
}

/// <summary>
/// Mazeret zaman çizelgesine eklenen işlemin türünü belirtir.
/// </summary>
public enum ExcuseEntryType
{
    StoreResponse,
    RevisionRequest,
    Approval
}

/// <summary>
/// Mağazanın fire farkı için bildirebileceği standart nedenlerdir.
/// </summary>
public enum ExcuseReasonType
{
    CountingDifference,
    ShipmentOrWaybill,
    SpoilageOrPhysicalWaste,
    IncorrectStockMovement,
    Return,
    PackagingOrProduction,
    Other
}
