using OzkFireTakibiClient.Src.Data;
using Microsoft.EntityFrameworkCore;
using OzkFireTakibiClient.Src.Data.Entities;

namespace OzkFireTakibiClient.Src.Services;

public class UserService(AppDbContext dbContext) : BaseService(dbContext)
{
    public virtual async Task<string> GetUserRoleAsync(int userId)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

        return user?.Role ?? "Unknown";
    }

    public virtual Task<UserEntity?> GetUserByIdAsync(int id)
    {
        return _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
    }

    public virtual Task<UserEntity?> GetUserByEmailAsync(string email)
    {
        var normalizedEmail = email.Trim();
        return _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail && !u.IsDeleted);
    }

    public virtual Task<UserEntity?> LoginAsync(string email, string password)
    {
        var normalizedEmail = email.Trim();
        return _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Email == normalizedEmail &&
                u.Password == password &&
                !u.IsDeleted);
    }

    public virtual Task<List<UserEntity>> GetAllUsersAsync()
    {
        return _dbContext.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted)
            .ToListAsync();
    }

    public virtual async Task<bool> CreateUserAsync(UserEntity user)
    {
        var now = DateTime.UtcNow;
        user.CreatedAt = now;
        user.UpdatedAt = now;
        user.IsDeleted = false;

        await _dbContext.Users.AddAsync(user);
        return await _dbContext.SaveChangesAsync() > 0;
    }

    public virtual async Task<bool> DeleteUserAsync(int id)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

        if (user is null)
        {
            return false;
        }

        user.IsDeleted = true;
        user.UpdatedAt = DateTime.UtcNow;
        return await _dbContext.SaveChangesAsync() > 0;
    }
}
