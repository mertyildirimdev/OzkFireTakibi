using OzkFireTakibiClient.Data;

namespace OzkFireTakibiClient.Src.Services;

public abstract class BaseService(AppDbContext dbContext)
{
    protected readonly AppDbContext _dbContext = dbContext;
}