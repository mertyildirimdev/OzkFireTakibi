using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OzkFireTakibiClient.Src.Authorization;
using OzkFireTakibiClient.Src.Data;
using OzkFireTakibiClient.Src.Data.Entities;
using OzkFireTakibiClient.Src.Excuses;
using OzkFireTakibiClient.Src.Options;
using OzkFireTakibiClient.Src.ReportImports;

namespace OzkFireTakibiClient.Src.Services;

/// <summary>
/// Rapor satırlarına bağlı mazeretlerin sorgulama, cevap ve değerlendirme işlemlerini yönetir.
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
                request.Title.Contains(normalizedSearch) ||
                (request.ReportRow.StoreName != null && request.ReportRow.StoreName.Contains(normalizedSearch)) ||
                (request.ReportRow.CategoryName != null && request.ReportRow.CategoryName.Contains(normalizedSearch)) ||
                (request.ReportRow.CategoryCode != null && request.ReportRow.CategoryCode.Contains(normalizedSearch)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        pageNumber = Math.Min(pageNumber, totalPages);

        var rawItems = await query
            .OrderBy(request => request.Status == ExcuseStatus.RevisionRequested ? 0 :
                request.Status == ExcuseStatus.Open ? 1 :
                request.Status == ExcuseStatus.Answered ? 2 : 3)
            .ThenByDescending(request => request.ReportRow.ReportImport.EndDate)
            .ThenBy(request => request.Title)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(request => new
            {
                request.Id,
                request.Title,
                request.Source,
                TargetRowType = request.ReportRow.RowType,
                ReportName = request.ReportRow.ReportImport.OriginalFileName,
                PeriodEndDate = request.ReportRow.ReportImport.EndDate,
                StoreNumber = request.ReportRow.StoreNumber!.Value,
                StoreName = request.ReportRow.StoreName!,
                TargetCode = request.ReportRow.RowType == ReportRowType.StoreCategory
                    ? request.ReportRow.CategoryCode
                    : request.ReportRow.StockCode,
                TargetName = request.ReportRow.RowType == ReportRowType.StoreCategory
                    ? request.ReportRow.CategoryName
                    : request.ReportRow.StockName,
                BenchmarkRate = request.ReportRow.RowType == ReportRowType.StoreSummary
                    ? dbContext.ReportRows
                        .Where(row => row.ReportImportId == request.ReportRow.ReportImportId && row.RowType == ReportRowType.General)
                        .Select(row => row.WasteRate)
                        .FirstOrDefault()
                    : request.ReportRow.RowType == ReportRowType.StoreCategory
                        ? dbContext.ReportRows
                            .Where(row =>
                                row.ReportImportId == request.ReportRow.ReportImportId &&
                                row.RowType == ReportRowType.CategorySummary &&
                                row.CategoryCode == request.ReportRow.CategoryCode)
                            .Select(row => row.WasteRate)
                            .FirstOrDefault()
                        : null,
                StoreRate = request.ReportRow.WasteRate,
                request.ThresholdRate,
                request.Status,
                request.CreatedAt
            })
            .ToArrayAsync(cancellationToken);

        var items = rawItems.Select(item => new ExcuseListItem
        {
            Id = item.Id,
            Title = item.Title,
            Source = item.Source,
            TargetRowType = item.TargetRowType,
            ReportName = item.ReportName,
            PeriodEndDate = item.PeriodEndDate,
            StoreNumber = item.StoreNumber,
            StoreName = item.StoreName ?? item.StoreNumber.ToString(),
            TargetCode = item.TargetCode,
            TargetName = item.TargetName,
            BenchmarkRate = item.BenchmarkRate,
            StoreRate = item.StoreRate,
            ThresholdRate = item.ThresholdRate,
            DeviationPercent = CalculateDeviation(item.BenchmarkRate, item.StoreRate),
            Status = item.Status,
            CreatedAtUtc = item.CreatedAt
        }).ToArray();

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
            .Include(item => item.ReportRow)
                .ThenInclude(row => row.ReportImport)
            .Include(item => item.RequestedByUser)
            .Include(item => item.Entries)
                .ThenInclude(entry => entry.CreatedByUser)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new ExcuseNotFoundException(id);

        var targetRow = request.ReportRow;
        var reportImport = targetRow.ReportImport;
        var benchmarkRate = await GetBenchmarkRateAsync(dbContext, targetRow, cancellationToken);
        var (cumulativeBenchmarkRate, cumulativeStoreRate) = await GetCumulativeRatesAsync(
            dbContext,
            targetRow,
            reportImport,
            cancellationToken);

        var topCategories = targetRow.RowType == ReportRowType.StoreSummary
            ? await dbContext.ReportRows
                .AsNoTracking()
                .Where(row =>
                    row.ReportImportId == targetRow.ReportImportId &&
                    row.RowType == ReportRowType.StoreCategory &&
                    row.StoreNumber == targetRow.StoreNumber &&
                    (row.WasteAmount < 0m || row.WasteRate < 0m))
                .OrderBy(row => row.WasteAmount)
                .ThenBy(row => row.WasteRate)
                .Take(10)
                .Select(row => new ExcuseCategoryItem
                {
                    CategoryCode = row.CategoryCode,
                    CategoryName = row.CategoryName,
                    WasteRate = row.WasteRate,
                    WasteAmount = row.WasteAmount
                })
                .ToArrayAsync(cancellationToken)
            : [];

        var topProductsQuery = dbContext.ReportRows
            .AsNoTracking()
            .Where(row =>
                row.ReportImportId == targetRow.ReportImportId &&
                row.RowType == ReportRowType.StoreProduct &&
                row.StoreNumber == targetRow.StoreNumber &&
                (row.WasteAmount < 0m || row.WasteRate < 0m));
        if (targetRow.RowType == ReportRowType.StoreCategory)
        {
            topProductsQuery = topProductsQuery.Where(row => row.CategoryCode == targetRow.CategoryCode);
        }
        else if (targetRow.RowType == ReportRowType.StoreProduct)
        {
            topProductsQuery = topProductsQuery.Where(row =>
                row.CategoryCode == targetRow.CategoryCode &&
                row.StockCode == targetRow.StockCode);
        }

        var topProducts = await topProductsQuery
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
            MatchesStore(access, targetRow) &&
            request.Status is ExcuseStatus.Open or ExcuseStatus.RevisionRequested;

        return new ExcuseDetailResult
        {
            Id = request.Id,
            ReportImportId = targetRow.ReportImportId,
            Title = request.Title,
            Source = request.Source,
            TargetRowType = targetRow.RowType,
            ReportName = reportImport.OriginalFileName,
            StartDate = reportImport.StartDate,
            EndDate = reportImport.EndDate,
            StoreNumber = targetRow.StoreNumber!.Value,
            StoreName = targetRow.StoreName ?? targetRow.StoreNumber.Value.ToString(),
            TargetCode = targetRow.RowType == ReportRowType.StoreCategory ? targetRow.CategoryCode : targetRow.StockCode,
            TargetName = targetRow.RowType == ReportRowType.StoreCategory
                ? targetRow.CategoryName
                : targetRow.RowType == ReportRowType.StoreProduct ? targetRow.StockName : null,
            RequestNote = request.RequestNote,
            RequestedBy = request.RequestedByUser is null
                ? null
                : request.RequestedByUser.Name ?? request.RequestedByUser.Email,
            BenchmarkRate = benchmarkRate,
            StoreRate = targetRow.WasteRate,
            ThresholdRate = request.ThresholdRate,
            DeviationPercent = CalculateDeviation(benchmarkRate, targetRow.WasteRate),
            CumulativeBenchmarkRate = cumulativeBenchmarkRate,
            CumulativeStoreRate = cumulativeStoreRate,
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
            TopCategories = topCategories,
            TopProducts = topProducts,
            CanRespond = canRespond,
            CanReview = canReview.Succeeded && request.Status == ExcuseStatus.Answered
        };
    }

    public async Task<long> CreateManualRequestAsync(
        long reportRowId,
        string? note,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated(user);
        var authorization = await authorizationService.AuthorizeAsync(user, ReportPolicies.CanRequestExcuses);
        if (!authorization.Succeeded)
        {
            throw new UnauthorizedAccessException("Kategori veya ürün için mazeret isteme yetkiniz bulunmuyor.");
        }

        var normalizedNote = NormalizeOptionalMessage(note);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var row = await dbContext.ReportRows
            .Include(item => item.ReportImport)
            .SingleOrDefaultAsync(item => item.Id == reportRowId, cancellationToken)
            ?? throw new InvalidOperationException("Seçilen rapor satırı bulunamadı.");

        if (row.RowType is not (ReportRowType.StoreCategory or ReportRowType.StoreProduct) ||
            row.ReportImport.PeriodType != ReportPeriodType.Monthly ||
            !row.ReportImport.IsActive ||
            row.StoreNumber is null)
        {
            throw new InvalidOperationException("Yalnızca aktif aylık raporun mağaza × kategori veya mağaza × ürün satırından mazeret istenebilir.");
        }

        var isEligible = await dbContext.Stores
            .AnyAsync(store => store.Id == row.StoreNumber.Value && store.IsExcuseEligible, cancellationToken);
        if (!isEligible)
        {
            throw new InvalidOperationException("Seçilen mağaza mazeret kapsamı dışında.");
        }

        if (await dbContext.ExcuseRequests.AnyAsync(request => request.ReportRowId == row.Id, cancellationToken))
        {
            var targetLabel = row.RowType == ReportRowType.StoreProduct ? "ürün" : "alt kategori";
            throw new InvalidOperationException($"Bu mağaza ve {targetLabel} için zaten bir mazeret talebi bulunuyor.");
        }

        var targetName = row.RowType == ReportRowType.StoreProduct
            ? row.StockName ?? row.StockCode ?? "Ürün"
            : row.CategoryName ?? row.CategoryCode ?? "Alt kategori";
        var now = DateTime.UtcNow;
        var request = new ExcuseRequestEntity
        {
            ReportRowId = row.Id,
            Source = ExcuseSource.Manual,
            Title = $"{targetName} — {row.StoreName ?? row.StoreNumber.Value.ToString()}",
            RequestNote = normalizedNote,
            RequestedByUserId = GetUserId(user),
            Status = ExcuseStatus.Open,
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.ExcuseRequests.Add(request);
        await dbContext.SaveChangesAsync(cancellationToken);
        return request.Id;
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
            .Include(item => item.ReportRow)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new ExcuseNotFoundException(id);

        if (!MatchesStore(access, request.ReportRow))
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
        var request = await dbContext.ExcuseRequests.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
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
        return await dbContext.Stores.AsNoTracking().OrderBy(store => store.Name)
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

    private static async Task<decimal?> GetBenchmarkRateAsync(
        AppDbContext dbContext,
        ReportRowEntity targetRow,
        CancellationToken cancellationToken) => targetRow.RowType switch
    {
        ReportRowType.StoreSummary => await dbContext.ReportRows.AsNoTracking()
            .Where(row => row.ReportImportId == targetRow.ReportImportId && row.RowType == ReportRowType.General)
            .Select(row => row.WasteRate)
            .SingleOrDefaultAsync(cancellationToken),
        ReportRowType.StoreCategory => await dbContext.ReportRows.AsNoTracking()
            .Where(row =>
                row.ReportImportId == targetRow.ReportImportId &&
                row.RowType == ReportRowType.CategorySummary &&
                row.CategoryCode == targetRow.CategoryCode)
            .Select(row => row.WasteRate)
            .SingleOrDefaultAsync(cancellationToken),
        ReportRowType.StoreProduct => await dbContext.ReportRows.AsNoTracking()
            .Where(row =>
                row.ReportImportId == targetRow.ReportImportId &&
                row.RowType == ReportRowType.ProductSummary &&
                row.StockCode == targetRow.StockCode)
            .Select(row => row.WasteRate)
            .SingleOrDefaultAsync(cancellationToken),
        _ => null
    };

    private static async Task<(decimal? BenchmarkRate, decimal? StoreRate)> GetCumulativeRatesAsync(
        AppDbContext dbContext,
        ReportRowEntity targetRow,
        ReportImportEntity reportImport,
        CancellationToken cancellationToken)
    {
        var cumulativeImportId = await dbContext.ReportImports.AsNoTracking()
            .Where(item =>
                item.ReportPeriodId == reportImport.ReportPeriodId &&
                item.PeriodType == ReportPeriodType.Cumulative &&
                item.IsActive)
            .Select(item => (long?)item.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (!cumulativeImportId.HasValue)
        {
            return (null, null);
        }

        var benchmarkRate = targetRow.RowType switch
        {
            ReportRowType.StoreSummary => await dbContext.ReportRows.AsNoTracking()
                .Where(row => row.ReportImportId == cumulativeImportId.Value && row.RowType == ReportRowType.General)
                .Select(row => row.WasteRate)
                .SingleOrDefaultAsync(cancellationToken),
            ReportRowType.StoreCategory => await dbContext.ReportRows.AsNoTracking()
                .Where(row =>
                    row.ReportImportId == cumulativeImportId.Value &&
                    row.RowType == ReportRowType.CategorySummary &&
                    row.CategoryCode == targetRow.CategoryCode)
                .Select(row => row.WasteRate)
                .SingleOrDefaultAsync(cancellationToken),
            ReportRowType.StoreProduct => await dbContext.ReportRows.AsNoTracking()
                .Where(row =>
                    row.ReportImportId == cumulativeImportId.Value &&
                    row.RowType == ReportRowType.ProductSummary &&
                    row.StockCode == targetRow.StockCode)
                .Select(row => row.WasteRate)
                .SingleOrDefaultAsync(cancellationToken),
            _ => null
        };

        var storeRate = await dbContext.ReportRows.AsNoTracking()
            .Where(row =>
                row.ReportImportId == cumulativeImportId.Value &&
                row.RowType == targetRow.RowType &&
                row.StoreNumber == targetRow.StoreNumber &&
                (targetRow.RowType != ReportRowType.StoreCategory || row.CategoryCode == targetRow.CategoryCode) &&
                (targetRow.RowType != ReportRowType.StoreProduct ||
                    (row.CategoryCode == targetRow.CategoryCode && row.StockCode == targetRow.StockCode)))
            .Select(row => row.WasteRate)
            .SingleOrDefaultAsync(cancellationToken);
        return (benchmarkRate, storeRate);
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
            return query.Where(request => request.ReportRow.StoreNumber == access.StoreNumber.Value);
        }

        return !string.IsNullOrWhiteSpace(access.StoreName)
            ? query.Where(request => request.ReportRow.StoreName == access.StoreName)
            : query.Where(_ => false);
    }

    private static ExcuseAccess GetAccess(ClaimsPrincipal user)
    {
        var isStoreUser = user.IsInRole(UserRole.User.ToString());
        var storeNumberClaim = user.FindFirstValue("StoreNumber");
        var storeNumber = int.TryParse(storeNumberClaim, out var parsedStoreNumber) ? parsedStoreNumber : (int?)null;
        return new ExcuseAccess(isStoreUser, storeNumber, user.FindFirstValue("StoreName"));
    }

    private static bool MatchesStore(ExcuseAccess access, ReportRowEntity row) =>
        access.StoreNumber.HasValue
            ? access.StoreNumber.Value == row.StoreNumber
            : !string.IsNullOrWhiteSpace(access.StoreName) &&
              string.Equals(access.StoreName, row.StoreName, StringComparison.OrdinalIgnoreCase);

    private static decimal? CalculateDeviation(decimal? benchmarkRate, decimal? storeRate)
    {
        if (!benchmarkRate.HasValue || !storeRate.HasValue)
        {
            return null;
        }

        var benchmarkMagnitude = Math.Abs(benchmarkRate.Value);
        return benchmarkMagnitude == 0m
            ? 100m
            : ((Math.Abs(storeRate.Value) / benchmarkMagnitude) - 1m) * 100m;
    }

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

    private static string? NormalizeOptionalMessage(string? message)
    {
        var normalized = message?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
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
