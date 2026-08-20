namespace OzkFireTakibiClient.Data.Entities;

public class BaseEntity<T>
{
    public T Id { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class SoftDeleteEntity<T> : BaseEntity<T>
{
    public bool IsDeleted { get; set; } = false;
}
