using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using OzkFireTakibi.Dashboard.Authorization;
using OzkFireTakibi.Dashboard.Models;
using OzkFireTakibi.Dashboard.Data;
using OzkFireTakibi.Dashboard.Data.Entities;

namespace OzkFireTakibi.Dashboard.Services;

public sealed class ReportDataService(IDbContextFactory<AppDbContext> dbContextFactory, IMemoryCache memoryCache,
    AuthenticationStateProvider stateProvider)
{
    public async Task<IReadOnlyList<ReportPeriodOption>> GetPeriodsAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var user = await GetUserAsync();
        var accessibleRows = ApplyReadAccess(dbContext.ReportRows.AsNoTracking(), user);
        var periods = await dbContext.ReportPeriods.AsNoTracking()
            .Where(period => period.Imports.Any(import => import.IsActive &&
                accessibleRows.Any(row => row.ReportImportId == import.Id)))
            .OrderByDescending(x => x.EndDate).ThenBy(x => x.CategorySignature).ThenByDescending(x => x.Id)
            .ToArrayAsync(cancellationToken);
        var periodIds = periods.Select(x => x.Id).ToArray();
        var imports = await dbContext.ReportImports.AsNoTracking()
            .Where(x => periodIds.Contains(x.ReportPeriodId) && x.IsActive &&
                accessibleRows.Any(row => row.ReportImportId == x.Id))
            .OrderByDescending(x => x.Id)
            .ToArrayAsync(cancellationToken);

        var importIds = imports.Select(x => x.Id).ToArray();
        var scopeRows = await accessibleRows.Where(row => importIds.Contains(row.ReportImportId) &&
            (row.RowType == ReportRowType.CategorySummary || row.RowType == ReportRowType.StoreCategory))
            .Select(row => new { row.ReportImportId, row.CategoryName, row.CategoryCode }).Distinct().ToArrayAsync(cancellationToken);

        return periods.Select(period =>
            {
                var matches = imports.Where(x => x.ReportPeriodId == period.Id).ToArray();
                return new ReportPeriodOption(
                    period.Id,
                    period.EndDate,
                    ToOption(matches.FirstOrDefault(x => x.PeriodType == ReportPeriodType.Monthly)),
                    ToOption(matches.FirstOrDefault(x => x.PeriodType == ReportPeriodType.Cumulative)),
                    period.CategorySignature,
                    string.Join(", ", scopeRows.Where(row => matches.Any(import => import.Id == row.ReportImportId))
                        .Select(row => row.CategoryName is not null && row.CategoryCode is not null
                            ? $"{row.CategoryName} ({row.CategoryCode})" : row.CategoryName ?? row.CategoryCode ?? "Kategori")
                        .Distinct().Order())) ;
            })
            .Where(x => x.Monthly is not null || x.Cumulative is not null)
            .ToArray();
    }

    public async Task<ReportSnapshot> GetSnapshotAsync(long importId, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync();
        var isStore = user.IsInRole(UserRole.User.ToString());
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var importItem = await dbContext.ReportImports.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == importId && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Seçilen rapor artık aktif değil. Dönemi yeniden seçin.");
        // Kullanıcı kapsamı cache anahtarının parçasıdır; mağazalar ortak snapshot kullanmaz.
        var cacheKey = $"report-snapshot:{importId}:{(isStore ? $"store:{user.GetUserId()}:{user.FindFirstValue("StoreNumber")}:{user.FindFirstValue("StoreName")}" : "central")}";
        if (memoryCache.TryGetValue(cacheKey, out ReportSnapshot? cached) && cached is not null)
        {
            return cached;
        }

        var rows = await ApplyReadAccess(dbContext.ReportRows.AsNoTracking(), user)
            .Where(x => x.ReportImportId == importId)
            .OrderBy(x => x.SourceRowNumber)
            .ToArrayAsync(cancellationToken);

        var general = rows.SingleOrDefault(x => x.RowType == (isStore ? ReportRowType.StoreSummary : ReportRowType.General))
            ?? throw new UnauthorizedAccessException("Bu raporda görüntüleyebileceğiniz özet bulunmuyor. Mağaza eşlemenizi kontrol ettirin.");
        var categories = rows.Where(x => x.RowType == (isStore ? ReportRowType.StoreCategory : ReportRowType.CategorySummary)).ToArray();
        var products = rows.Where(x => x.RowType == (isStore ? ReportRowType.StoreProduct : ReportRowType.ProductSummary)).ToArray();
        var stores = rows.Where(x => x.RowType == ReportRowType.StoreProduct).ToArray();
        var categoryByStock = stores
            .GroupBy(ReportSnapshot.StockKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => ReportSnapshot.CategoryKey(group.First()), StringComparer.OrdinalIgnoreCase);

        var snapshot = new ReportSnapshot
        {
            IsStoreScoped = isStore,
            Import = ToOption(importItem)!,
            Rows = rows,
            General = general,
            Categories = categories,
            ProductsByCategory = products
                .GroupBy(product => categoryByStock.GetValueOrDefault(ReportSnapshot.StockKey(product), "(kategori-yok)"), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => (IReadOnlyList<ReportRowEntity>)x.ToArray(), StringComparer.OrdinalIgnoreCase),
            StoresByProduct = stores.GroupBy(ReportSnapshot.ProductKey)
                .ToDictionary(x => x.Key, x => (IReadOnlyList<ReportRowEntity>)x.ToArray(), StringComparer.OrdinalIgnoreCase),
            StoreProducts = stores
        };

        memoryCache.Set(cacheKey, snapshot, TimeSpan.FromMinutes(20));
        return snapshot;
    }

    public async Task<MonthlyOverview> GetMonthlyOverviewAsync(DateOnly month, decimal multiplier,
        CancellationToken cancellationToken = default)
    {
        if (multiplier <= 1m) throw new ArgumentOutOfRangeException(nameof(multiplier));
        month = new DateOnly(month.Year, month.Month, 1);
        var nextMonth = month.AddMonths(1);
        var previousMonth = month.AddMonths(-1);
        var user = await GetUserAsync();
        var isStore = user.IsInRole(UserRole.User.ToString());
        var periods = await GetPeriodsAsync(cancellationToken);

        // Bir kapsam, ilk görüldüğü aydan itibaren takip edilir. Gelecekte eklenen kapsamlar
        // geçmiş aylarda eksik rapor sayılmaz; geçmiş ayın rakamları bu aya taşınmaz.
        var scopes = periods.Where(x => x.EndDate < nextMonth)
            .GroupBy(x => x.CategorySignature, StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                Label = group.First().ScopeLabel,
                Current = group.FirstOrDefault(x => x.EndDate >= month),
                Previous = group.FirstOrDefault(x => x.EndDate >= previousMonth && x.EndDate < month)
            }).OrderBy(x => x.Label, StringComparer.CurrentCultureIgnoreCase).ToArray();
        var imports = scopes.SelectMany(x => new[] { x.Current?.Monthly, x.Previous?.Monthly })
            .OfType<ReportImportOption>().DistinctBy(x => x.Id).ToArray();
        var importIds = imports.Select(x => x.Id).ToArray();
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        // Ana sayfa ürün detaylarını yüklemez; bütün grupların özetleri tek sorguyla alınır.
        var summaries = await ApplyReadAccess(db.ReportRows.AsNoTracking(), user)
            .Where(x => importIds.Contains(x.ReportImportId) && x.ReportImport.IsActive &&
                (x.RowType == ReportRowType.General || x.RowType == ReportRowType.StoreSummary))
            .ToArrayAsync(cancellationToken);
        var benchmarks = await db.ReportRows.AsNoTracking()
            .Where(x => importIds.Contains(x.ReportImportId) && x.ReportImport.IsActive && x.RowType == ReportRowType.General)
            .Select(x => new { x.ReportImportId, x.WasteRate }).ToDictionaryAsync(x => x.ReportImportId, x => x.WasteRate, cancellationToken);
        var eligible = await db.Stores.AsNoTracking().Where(x => x.IsExcuseEligible)
            .Select(x => x.Id).ToHashSetAsync(cancellationToken);
        var currentImportIds = scopes.Select(x => x.Current?.Monthly?.Id).OfType<long>().ToHashSet();
        var attentionRows = summaries.Where(x => currentImportIds.Contains(x.ReportImportId) &&
            x.RowType == ReportRowType.StoreSummary && x.StoreNumber.HasValue && eligible.Contains(x.StoreNumber.Value) &&
            x.WasteRate is < 0m && benchmarks.GetValueOrDefault(x.ReportImportId) is { } benchmark &&
            Math.Abs(x.WasteRate.Value) >= Math.Abs(benchmark) * multiplier).ToArray();
        var attentionIds = attentionRows.Select(x => x.Id).ToArray();
        var requests = await db.ExcuseRequests.AsNoTracking().Where(x => attentionIds.Contains(x.ReportRowId) &&
            x.Status != ExcuseStatus.Superseded).OrderByDescending(x => x.Id)
            .Select(x => new { x.Id, x.ReportRowId, x.Status }).ToArrayAsync(cancellationToken);
        var groups = new List<OverviewReportGroup>();
        var storeReports = new List<OverviewStoreReport>();
        foreach (var scope in scopes)
        {
            var current = scope.Current?.Monthly;
            var previous = scope.Previous?.Monthly;
            var summaryType = isStore ? ReportRowType.StoreSummary : ReportRowType.General;
            var summary = summaries.SingleOrDefault(x => x.ReportImportId == current?.Id && x.RowType == summaryType);
            var previousSummary = current?.StartDate == month && current.EndDate == nextMonth.AddDays(-1) &&
                previous?.StartDate == previousMonth && previous.EndDate == month.AddDays(-1)
                ? summaries.SingleOrDefault(x => x.ReportImportId == previous.Id && x.RowType == summaryType) : null;
            var groupRows = attentionRows.Where(x => x.ReportImportId == current?.Id).ToArray();
            var label = string.IsNullOrWhiteSpace(scope.Label) ? "Kategori kapsamı belirtilmemiş" : scope.Label;
            groups.Add(new OverviewReportGroup(label, scope.Current, summary, previousSummary,
                current is null ? null : benchmarks.GetValueOrDefault(current.Id), groupRows.Length));
            foreach (var row in groupRows)
            {
                var request = requests.FirstOrDefault(x => x.ReportRowId == row.Id);
                storeReports.Add(new OverviewStoreReport(label, scope.Current!.Id, row, request?.Id, request?.Status));
            }
        }
        // Çakışan kategori kapsamlarını toplamak kaybı iki kez sayabilir. Öncelik önce
        // etkilenen grup sayısı, sonra tek bir gruptaki en büyük kayıp üzerinden belirlenir.
        var stores = storeReports.GroupBy(x => x.Row.StoreNumber!.Value)
            .Select(group => new OverviewStore(group.Key, group.First().Row.StoreName ?? group.Key.ToString(),
                group.OrderByDescending(x => Math.Abs(x.Row.WasteAmount ?? 0m)).ThenBy(x => x.GroupLabel).ToArray()))
            .OrderByDescending(x => x.Reports.Count)
            .ThenByDescending(x => x.Reports.Max(report => Math.Abs(report.Row.WasteAmount ?? 0m)))
            .ThenBy(x => x.StoreNumber).ToArray();
        return new MonthlyOverview(month, isStore, groups, stores);
    }

    private async Task<ClaimsPrincipal> GetUserAsync()
    {
        var user = (await stateProvider.GetAuthenticationStateAsync()).User;
        user.EnsureAuthenticated();
        if (!Enum.GetNames<UserRole>().Any(user.IsInRole)) throw new UnauthorizedAccessException("Rapor görüntüleme yetkiniz bulunmuyor.");
        return user;
    }

    private static IQueryable<ReportRowEntity> ApplyReadAccess(IQueryable<ReportRowEntity> rows, ClaimsPrincipal user)
    {
        if (!user.IsInRole(UserRole.User.ToString())) return rows;
        if (int.TryParse(user.FindFirstValue("StoreNumber"), out var number)) return rows.Where(x => x.StoreNumber == number);
        var name = user.FindFirstValue("StoreName");
        return string.IsNullOrWhiteSpace(name) ? rows.Where(_ => false) : rows.Where(x => x.StoreName == name);
    }

    private static ReportImportOption? ToOption(ReportImportEntity? item) => item is null
        ? null
        : new(item.Id, item.PeriodType, item.StartDate, item.EndDate, item.OriginalFileName);
}
