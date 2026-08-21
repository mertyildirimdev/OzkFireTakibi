using OzkFireTakibiClient.Src.Data;

namespace OzkFireTakibiClient.Src.Services;

/// <summary>
/// Veritabanı erişimi gerektiren iş mantığı servisleri için ortak temel sınıf.
/// </summary>
public abstract class BaseService(AppDbContext dbContext)
{
    /// <summary>
    /// Veritabanı bağlamı örneği
    /// </summary>
    protected readonly AppDbContext _dbContext = dbContext;
}