using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OzkFireTakibiClient.Src.Authorization;
using OzkFireTakibiClient.Src.Data;
using OzkFireTakibiClient.Src.Data.Entities;
using OzkFireTakibiClient.Src.Excuses;
using OzkFireTakibiClient.Src.Options;

namespace OzkFireTakibiClient.Src.Services;

/// <summary>
/// Mazeret taleplerinin yetkilendirilmiş sorgulama, cevap ve değerlendirme işlemlerini yönetir.
/// </summary>
public sealed class ExcuseService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    IAuthorizationService authorizationService,
    IOptions<ExcuseOptions> options)
{
    private readonly ExcuseOptions _options = options.Value;

    public async Task<ExcuseListResult> GetListAsync(
        ClaimsPrincipal user,
        ExcuseStatus? status,
        string? searchText,
        int pageNumber,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated(user);
        pageNumber = Math.Max(1, pageNumber);
        var pageSize = Math.Clamp(_options.PageSize, 10, 100);
        var normalizedSearch = searchText?.Trim();
        var access = GetAccess(user);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var accessibleQuery = ApplyReadAccess(dbContext.ExcuseRequests.AsNoTracking(), access);

        var activeQuery = accessibleQuery.Where(request => request.Status != ExcuseStatus.Superseded);
        var openCount = await activeQuery.CountAsync(request => request.Status == ExcuseStatus.Open, cancellationToken);
        var answeredCount = await activeQuery.CountAsync(request => request.Status == ExcuseStatus.Answered, cancellationToken);
        var revisionCount = await activeQuery.CountAsync(request => request.Status == ExcuseStatus.RevisionRequested, cancellationToken);
        var approvedCount = await activeQuery.CountAsync(request => request.Status == ExcuseStatus.Approved, cancellationToken);

        var query = status.HasValue
            ? accessibleQuery.Where(request => request.Status == status.Value)
            : activeQuery;

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(request =>
                request.StoreName.Contains(normalizedSearch) ||
                request.CategoryName.Contains(normalizedSearch) ||
                request.CategoryCode.Contains(normalizedSearch));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        pageNumber = Math.Min(pageNumber, totalPages);

        var items = await query
            .OrderBy(request => request.Status == ExcuseStatus.RevisionRequested ? 0 :
                request.Status == ExcuseStatus.Open ? 1 :
                request.Status == ExcuseStatus.Answered ? 2 : 3)
            .ThenByDescending(request => request.ReportImport.EndDate)
            .ThenBy(request => request.StoreName)
            .ThenBy(request => request.CategoryName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(request => new ExcuseListItem
            {
                Id = request.Id,
                Scope = request.ReportImport.Scope,
                PeriodEndDate = request.ReportImport.EndDate,
                StoreNumber = request.StoreNumber,
                StoreName = request.StoreName,
                CategoryCode = request.CategoryCode,
                CategoryName = request.CategoryName,
                CategoryAverageWasteRate = request.CategoryAverageWasteRate,
                StoreWasteRate = request.StoreWasteRate,
                DeviationPercent = request.DeviationPercent,
                Status = request.Status,
                CreatedAtUtc = request.CreatedAt
            })
            .ToArrayAsync(cancellationToken);

        return new ExcuseListResult
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            OpenCount = openCount,
            AnsweredCount = answeredCount,
            RevisionRequestedCount = revisionCount,
            ApprovedCount = approvedCount,
            IsStoreUser = access.IsStoreUser,
            StoreNumber = access.StoreNumber,
            StoreName = access.StoreName
        };
    }

    public async Task<ExcuseDetailResult> GetDetailAsync(
        long id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated(user);
        var access = GetAccess(user);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var request = await ApplyReadAccess(dbContext.ExcuseRequests.AsNoTracking(), access)
            .Include(item => item.ReportImport)
            .Include(item => item.Entries)
                .ThenInclude(entry => entry.CreatedByUser)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new ExcuseNotFoundException(id);

        var topProducts = await dbContext.ReportRows
            .AsNoTracking()
            .Where(row =>
                row.ReportImportId == request.ReportImportId &&
                row.RowType == ReportRowType.StoreProduct &&
                row.StoreNumber == request.StoreNumber &&
                row.CategoryCode == request.CategoryCode &&
                (row.WasteAmount < 0m || row.WasteRate < 0m))
            .OrderBy(row => row.WasteAmount)
            .ThenBy(row => row.WasteRate)
            .Take(10)
            .Select(row => new ExcuseProductItem
            {
                StockCode = row.StockCode,
                StockName = row.StockName,
                WasteRate = row.WasteRate,
                WasteAmount = row.WasteAmount
            })
            .ToArrayAsync(cancellationToken);

        var canReview = await authorizationService.AuthorizeAsync(user, ReportPolicies.CanReviewExcuses);
        var canRespond = access.IsStoreUser &&
            MatchesStore(access, request) &&
            request.Status is ExcuseStatus.Open or ExcuseStatus.RevisionRequested;

        return new ExcuseDetailResult
        {
            Id = request.Id,
            ReportImportId = request.ReportImportId,
            Scope = request.ReportImport.Scope,
            StartDate = request.ReportImport.StartDate,
            EndDate = request.ReportImport.EndDate,
            StoreNumber = request.StoreNumber,
            StoreName = request.StoreName,
            CategoryCode = request.CategoryCode,
            CategoryName = request.CategoryName,
            CategoryAverageWasteRate = request.CategoryAverageWasteRate,
            StoreWasteRate = request.StoreWasteRate,
            ThresholdWasteRate = request.ThresholdWasteRate,
            DeviationPercent = request.DeviationPercent,
            Status = request.Status,
            Entries = request.Entries
                .OrderBy(entry => entry.CreatedAt)
                .Select(entry => new ExcuseEntryItem
                {
                    EntryType = entry.EntryType,
                    ReasonType = entry.ReasonType,
                    Message = entry.Message,
                    CreatedBy = entry.CreatedByUser.Name ?? entry.CreatedByUser.Email,
                    CreatedAtUtc = entry.CreatedAt
                })
                .ToArray(),
            TopProducts = topProducts,
            CanRespond = canRespond,
            CanReview = canReview.Succeeded && request.Status == ExcuseStatus.Answered
        };
    }

    public async Task SubmitResponseAsync(
        long id,
        ExcuseReasonType reasonType,
        string message,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated(user);
        var access = GetAccess(user);
        var normalizedMessage = ValidateMessage(message, 10);
        if (!access.IsStoreUser || (access.StoreNumber is null && string.IsNullOrWhiteSpace(access.StoreName)))
        {
            throw new UnauthorizedAccessException("Mazeret yanıtı yalnızca mağazaya bağlı kullanıcı tarafından gönderilebilir.");
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var request = await dbContext.ExcuseRequests
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new ExcuseNotFoundException(id);

        if (!MatchesStore(access, request))
        {
            throw new UnauthorizedAccessException("Bu mazeret talebi kullanıcının bağlı olduğu mağazaya ait değil.");
        }

        if (request.Status is not (ExcuseStatus.Open or ExcuseStatus.RevisionRequested))
        {
            throw new InvalidOperationException("Bu mazeret talebi şu anda yanıtlanamaz.");
        }

        var now = DateTime.UtcNow;
        dbContext.ExcuseEntries.Add(new ExcuseEntryEntity
        {
            ExcuseRequestId = request.Id,
            CreatedByUserId = GetUserId(user),
            EntryType = ExcuseEntryType.StoreResponse,
            ReasonType = reasonType,
            Message = normalizedMessage,
            CreatedAt = now,
            UpdatedAt = now
        });
        request.Status = ExcuseStatus.Answered;
        request.RespondedAtUtc = now;
        request.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReviewAsync(
        long id,
        bool approve,
        string? message,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated(user);
        var authorization = await authorizationService.AuthorizeAsync(user, ReportPolicies.CanReviewExcuses);
        if (!authorization.Succeeded)
        {
            throw new UnauthorizedAccessException("Mazeret değerlendirme yetkiniz bulunmuyor.");
        }

        var normalizedMessage = approve
            ? string.IsNullOrWhiteSpace(message) ? "Mazeret uygun bulundu." : ValidateMessage(message, 1)
            : ValidateMessage(message, 5);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var request = await dbContext.ExcuseRequests
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new ExcuseNotFoundException(id);

        if (request.Status != ExcuseStatus.Answered)
        {
            throw new InvalidOperationException("Yalnızca yanıtlanmış bir mazeret değerlendirilebilir.");
        }

        var now = DateTime.UtcNow;
        dbContext.ExcuseEntries.Add(new ExcuseEntryEntity
        {
            ExcuseRequestId = request.Id,
            CreatedByUserId = GetUserId(user),
            EntryType = approve ? ExcuseEntryType.Approval : ExcuseEntryType.RevisionRequest,
            Message = normalizedMessage,
            CreatedAt = now,
            UpdatedAt = now
        });
        request.Status = approve ? ExcuseStatus.Approved : ExcuseStatus.RevisionRequested;
        request.ReviewedAtUtc = now;
        request.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExcuseStoreItem>> GetStoresAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        await EnsureStoreManagementAsync(user);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Stores
            .AsNoTracking()
            .OrderBy(store => store.Name)
            .Select(store => new ExcuseStoreItem
            {
                StoreNumber = store.Id,
                StoreName = store.Name,
                IsExcuseEligible = store.IsExcuseEligible
            })
            .ToArrayAsync(cancellationToken);
    }

    public async Task SetStoreEligibilityAsync(
        int storeNumber,
        bool isEligible,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        await EnsureStoreManagementAsync(user);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var store = await dbContext.Stores.SingleOrDefaultAsync(item => item.Id == storeNumber, cancellationToken)
            ?? throw new InvalidOperationException("Mağaza bulunamadı.");
        store.IsExcuseEligible = isEligible;
        store.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureStoreManagementAsync(ClaimsPrincipal user)
    {
        EnsureAuthenticated(user);
        var authorization = await authorizationService.AuthorizeAsync(user, ReportPolicies.CanManageExcuseStores);
        if (!authorization.Succeeded)
        {
            throw new UnauthorizedAccessException("Mağaza kapsamını yönetme yetkiniz bulunmuyor.");
        }
    }

    private static IQueryable<ExcuseRequestEntity> ApplyReadAccess(
        IQueryable<ExcuseRequestEntity> query,
        ExcuseAccess access)
    {
        if (!access.IsStoreUser)
        {
            return query;
        }

        if (access.StoreNumber.HasValue)
        {
            return query.Where(request => request.StoreNumber == access.StoreNumber.Value);
        }

        return !string.IsNullOrWhiteSpace(access.StoreName)
            ? query.Where(request => request.StoreName == access.StoreName)
            : query.Where(_ => false);
    }

    private static ExcuseAccess GetAccess(ClaimsPrincipal user)
    {
        var isStoreUser = user.IsInRole(UserRole.User.ToString());
        var storeNumberClaim = user.FindFirstValue("StoreNumber");
        var storeNumber = int.TryParse(storeNumberClaim, out var parsedStoreNumber)
            ? parsedStoreNumber
            : (int?)null;
        return new ExcuseAccess(isStoreUser, storeNumber, user.FindFirstValue("StoreName"));
    }

    private static bool MatchesStore(ExcuseAccess access, ExcuseRequestEntity request) =>
        access.StoreNumber.HasValue
            ? access.StoreNumber.Value == request.StoreNumber
            : !string.IsNullOrWhiteSpace(access.StoreName) &&
              string.Equals(access.StoreName, request.StoreName, StringComparison.OrdinalIgnoreCase);

    private static string ValidateMessage(string? message, int minimumLength)
    {
        var normalized = message?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length < minimumLength)
        {
            throw new InvalidOperationException($"Açıklama en az {minimumLength} karakter olmalıdır.");
        }

        if (normalized.Length > 2000)
        {
            throw new InvalidOperationException("Açıklama en fazla 2000 karakter olabilir.");
        }

        return normalized;
    }

    private static void EnsureAuthenticated(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("Mazeretleri görüntülemek için oturum açılmalıdır.");
        }
    }

    private static int GetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Kullanıcı kimliği doğrulanamadı.");
    }

    private sealed record ExcuseAccess(bool IsStoreUser, int? StoreNumber, string? StoreName);
}
