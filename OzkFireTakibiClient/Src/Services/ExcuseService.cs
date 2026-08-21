using OzkFireTakibiClient.Src.Data;

namespace OzkFireTakibiClient.Src.Services;

/// <summary>
/// Fire ve stok sapmaları için mazeret/açıklama kayıtlarını yöneten servis.
/// </summary>
public class ExcuseService(AppDbContext dbContext) : BaseService(dbContext)
{
}