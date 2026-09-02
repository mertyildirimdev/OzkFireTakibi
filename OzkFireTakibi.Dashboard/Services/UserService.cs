using Microsoft.EntityFrameworkCore;
using OzkFireTakibi.Dashboard.Data;

namespace OzkFireTakibi.Dashboard.Services;

public sealed class UserService(IDbContextFactory<ReportDbContext> dbContextFactory)
{
    public async Task<UserRecord?> GetByIdAsync(int id)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == id && !user.IsDeleted);
    }

    public async Task<UserRecord?> LoginAsync(string email, string password)
    {
        var normalizedEmail = email.Trim();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var user = await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(user => user.Email == normalizedEmail && !user.IsDeleted);

        return user is not null && user.Password == password ? user : null;
    }
}
