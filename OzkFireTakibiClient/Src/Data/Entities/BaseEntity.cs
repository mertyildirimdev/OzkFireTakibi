namespace OzkFireTakibiClient.Src.Data.Entities;

/// <summary>
/// Tüm veritabanı varlıkları için birincil anahtar (Id) ve zaman damgası (CreatedAt, UpdatedAt) alanlarını tanımlayan temel sınıf.
/// </summary>
/// <typeparam name="T">Birincil anahtarın veri türü (int, long vb.)</typeparam>
public class BaseEntity<T>
{
    /// <summary>
    /// Varlığın benzersiz kimliği (Primary Key)
    /// </summary>
    public T Id { get; set; } = default!;

    /// <summary>
    /// Kaydın oluşturulma zamanı (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Kaydın son güncellenme zamanı (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Mantıksal silme (soft delete) desteğine sahip varlıklar için temel sınıf.
/// </summary>
/// <typeparam name="T">Birincil anahtarın veri türü</typeparam>
public class SoftDeleteEntity<T> : BaseEntity<T>
{
    /// <summary>
    /// Kaydın mantıksal olarak silinip silinmediğini belirtir
    /// </summary>
    public bool IsDeleted { get; set; } = false;
}

