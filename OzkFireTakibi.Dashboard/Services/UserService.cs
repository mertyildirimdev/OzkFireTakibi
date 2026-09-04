using Microsoft.EntityFrameworkCore;
using OzkFireTakibi.Dashboard.Data;
using OzkFireTakibi.Dashboard.Data.Entities;

namespace OzkFireTakibi.Dashboard.Services;

public sealed class UserService(IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<UserEntity?> GetByIdAsync(int id)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == id && !user.IsDeleted);
    }

    public async Task<UserEntity?> LoginAsync(string email, string password)
    {
        var normalizedEmail = email.Trim();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var user = await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(user => user.Email == normalizedEmail && !user.IsDeleted);

        return user is not null && user.Password == password ? user : null;
    }
}
