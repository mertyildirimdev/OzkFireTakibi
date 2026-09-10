// Gerçek veritabanına bağlanmaz; uygulamanın servislerini EF InMemory ve örnek verilerle doğrular.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OzkFireTakibi.Dashboard.Data;
using OzkFireTakibi.Dashboard.Data.Entities;
using OzkFireTakibi.Dashboard.Models;
using OzkFireTakibi.Dashboard.Options;
using OzkFireTakibi.Dashboard.Services;
using OzkFireTakibi.Dashboard.Components.Pages;
using OzkFireTakibi.Dashboard.Components.Report;

public static class DashboardChecks
{
    private static int checks;
    private static void Check(bool ok, string name) { if (!ok) throw new Exception(name); Console.WriteLine("PASS " + name); checks++; }
    public static ClaimsPrincipal User(string role, int? store = null, string? name = null) => new(new ClaimsIdentity(
        new[] { new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(ClaimTypes.Role, role) }
            .Concat(store.HasValue ? new[] { new Claim("StoreNumber", store.ToString()!) } : Array.Empty<Claim>())
            .Concat(name is not null ? new[] { new Claim("StoreName", name) } : Array.Empty<Claim>()), "test"));

    public static async Task Main(string[] args)
    {
        var factory = new TestFactory();
        await Seed(factory);
        var state = new TestState { User = User("Admin") };
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var data = new ReportDataService(factory, cache, state);
        var periods = await data.GetPeriodsAsync();
        Check(periods.Count == 3 && periods[0].Label != periods[1].Label, "Same-month category scopes have distinct labels");
        var central = await data.GetSnapshotAsync(1);
        Check(central.General.WasteAmount == -10000 && central.Rows.Any(x => x.StoreNumber == 65), "Central report retains all stores");
        var attention = (await data.GetMonthlyOverviewAsync(new DateOnly(2026, 5, 1), 1.5m)).Stores.SelectMany(x => x.Reports).Where(x => x.PeriodId == 1).ToArray();
        Check(attention.Any(x => x.Row.StoreNumber == 1) && attention.All(x => x.Row.StoreNumber != 2), "Threshold equality matches automation; excluded stores are omitted");
        Check(attention.First().RequestId.HasValue, "Priority stores link to their request");
        state.User = User("User", 1);
        var scoped = await data.GetSnapshotAsync(1);
        Check(scoped.IsStoreScoped && scoped.Rows.All(x => x.StoreNumber == 1) && scoped.General.WasteAmount == -101, "Store query and shared cache do not expose other stores or general totals");
        Check(scoped.Categories.All(x => x.StoreNumber == 1), "Store categories use store summaries");
        state.User = User("User", 2);
        Check((await data.GetSnapshotAsync(1)).Rows.All(x => x.StoreNumber == 2), "Store change uses separate cached data");
        state.User = User("User", name: "Mağaza 01");
        Check((await data.GetSnapshotAsync(1)).Rows.All(x => x.StoreNumber == 1), "Legacy name-based store mapping is scoped");
        state.User = User("User");
        Check((await data.GetPeriodsAsync()).Count == 0, "Unmapped store has no report periods");
        try { await data.GetSnapshotAsync(1); Check(false, "Unmapped store denied"); } catch (UnauthorizedAccessException) { Check(true, "Unmapped store denied"); }
        state.User = new ClaimsPrincipal(new ClaimsIdentity());
        try { await data.GetPeriodsAsync(); Check(false, "Anonymous denied"); } catch (UnauthorizedAccessException) { Check(true, "Anonymous denied"); }
        state.User = User("Admin");
        var services = new ServiceCollection().AddLogging().AddAuthorizationCore(options =>
            options.AddPolicy("CanImportReports", policy => policy.RequireRole("Admin")));
        services.AddSingleton<AuthenticationStateProvider>(state);
        services.AddSingleton<IDbContextFactory<AppDbContext>>(factory);
        services.AddSingleton<IMemoryCache>(cache);
        services.AddSingleton(data);
        services.AddSingleton<IOptions<ExcuseOptions>>(Options.Create(new ExcuseOptions { PageSize = 10 }));
        services.AddSingleton<ExcuseService>();
        services.AddSingleton<NavigationManager>(new TestNavigation());
        using var provider = services.BuildServiceProvider();
        var excuses = provider.GetRequiredService<ExcuseService>();
        var first = await excuses.GetListAsync(state.User, null, null, 1, periodId: 1);
        var second = await excuses.GetListAsync(state.User, null, null, 2, periodId: 1);
        Check(first.TotalCount == 66 && first.Items.Count == 10 && !first.Items.Select(x => x.Id).Intersect(second.Items.Select(x => x.Id)).Any(), "Pagination covers records beyond first page without duplicates");
        var search = await excuses.GetListAsync(state.User, null, "65", 1, periodId: 1);
        Check(search.Items.Count == 1 && search.Items[0].StoreNumber == 65, "Store search finds record beyond first page");
        Check((await excuses.GetListAsync(state.User, null, "SON-URUN", 1, periodId: 1)).TotalCount == 1, "Product-code search runs in database query");
        Check((await excuses.GetListAsync(state.User, null, "Mayıs", 1, periodId: 1)).TotalCount == 66, "Report-name search runs in database query");
        Check((await excuses.GetListAsync(state.User, ExcuseStatus.Answered, null, 1, periodId: 1)).TotalCount == 1, "Status filter reaches records beyond first page");
        Check((await excuses.GetListAsync(state.User, null, null, 1, periodId: 2)).TotalCount == 0, "Dashboard period links keep work list scoped");
        Check((await excuses.GetListAsync(User("User", 1), null, null, 1)).Items.All(x => x.StoreNumber == 1), "Work list preserves store access");
        var may = new DateOnly(2026, 5, 1);
        var overview = await data.GetMonthlyOverviewAsync(may, 1.5m);
        Check(overview.Groups.Count == 2 && overview.ReceivedCount == 2 && overview.MissingCount == 0, "Monthly overview includes all current groups");
        Check(overview.Stores.Count == 64 && overview.Groups.Sum(x => x.AttentionStoreCount) == 65, "One store in two groups is counted once in overall risk count");
        Check(overview.Stores.First().StoreNumber == 1 && overview.Stores.First().Reports.Count == 2, "Multi-group store takes priority over larger single-group loss");
        Check(overview.ComparableCount == 1, "Only matching previous-month group is compared");
        Check((await data.GetMonthlyOverviewAsync(may.AddMonths(-1), 1.5m)).Groups.Count == 1, "Groups introduced later are not missing in historical months");
        state.User = User("User", 1);
        var storeOverview = await data.GetMonthlyOverviewAsync(may, 1.5m);
        Check(storeOverview.Stores.Count == 1 && storeOverview.Groups.All(x => x.Summary?.StoreNumber == 1), "Cross-group overview preserves store access");
        state.User = User("Admin");
        await using (var extra = factory.CreateDbContext())
        {
            var secondGroupRow = await extra.ReportRows.FirstAsync(x => x.ReportImportId == 2 && x.RowType == ReportRowType.StoreSummary);
            var aprilRow = await extra.ReportRows.FirstAsync(x => x.ReportImportId == 3 && x.RowType == ReportRowType.StoreSummary);
            extra.ExcuseRequests.Add(new ExcuseRequestEntity { Id = 67, ReportRow = secondGroupRow, Title = "Manav açıklaması", Status = ExcuseStatus.RevisionRequested });
            extra.ExcuseRequests.Add(new ExcuseRequestEntity { Id = 68, ReportRow = aprilRow, Title = "Nisan açıklaması", Status = ExcuseStatus.Open });
            await extra.SaveChangesAsync();
        }
        var monthWork = await excuses.GetListAsync(state.User, null, null, 1, month: may);
        Check(monthWork.TotalCount == 67 && monthWork.RevisionRequestedCount == 1, "Month work list and counters include different report groups");
        Check((await excuses.GetListAsync(state.User, null, null, 1, month: may.AddMonths(-1))).TotalCount == 1, "Month work filter excludes other months");
        await AddScope(factory, 4, new DateOnly(2026, 4, 30), "C", "Unlu mamuller");
        var missing = await data.GetMonthlyOverviewAsync(may, 1.5m);
        Check(missing.Groups.Count == 3 && missing.MissingCount == 1 && missing.Groups.Single(x => x.Label.Contains("Unlu")).Summary is null, "Historical group without current report is explicitly missing with no carried totals");
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<Home>()).ToHtmlString());
        var decoded = System.Net.WebUtility.HtmlDecode(html);
        Check(decoded.Contains("Öncelikli mağazalar") && decoded.Contains("₺") && decoded.Contains("Değişmedi") && decoded.Contains("Manav") && decoded.Contains("Şarküteri"), "Home renders all report groups and previous-month comparisons together");
        Check(decoded.Contains("has-text-danger\">%-5,00"), "Negative profit is styled as loss");
        var gridHtml = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<ReportTreeGrid>(ParameterView.FromDictionary(new Dictionary<string, object?>
        { ["Snapshot"] = central, ["Columns"] = ReportColumnCatalog.All.Where(x => x.IsDefault).ToArray() }))).ToHtmlString());
        Check(gridHtml.Contains("pagination-next") && !System.Net.WebUtility.HtmlDecode(gridHtml).Contains("Mağaza 65</strong>"), "Analysis renders first 50 store rows and pagination");
        state.User = User("User", 1);
        var storeHtml = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<Home>()).ToHtmlString());
        Check(!System.Net.WebUtility.HtmlDecode(storeHtml).Contains("Mağaza 65") && storeHtml.Contains("101,00"), "Store homepage renders only own totals and work");
        state.User = User("Admin");
        File.WriteAllText(Path.Combine(args[0], "home.html"), "<!doctype html><html lang='tr' data-theme='light'><meta charset='utf-8'><meta name='viewport' content='width=device-width, initial-scale=1'><link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/bulma@1.0.4/css/bulma.min.css'><body><main class='section'>" + html + "</main></body></html>");
        await AddScope(factory, 5, new DateOnly(2026, 6, 30), "D", "Yeni kapsam");
        var historical = await data.GetMonthlyOverviewAsync(may, 1.5m);
        Check(historical.Groups.Count == 3 && historical.Groups.All(x => !x.Label.Contains("Yeni kapsam")), "Future scope does not inflate historical missing count");
        await using var db = factory.CreateDbContext();
        (await db.ReportImports.FindAsync(5L))!.IsActive = false;
        (await db.ReportImports.FindAsync(1L))!.StartDate = may.AddDays(1);
        await db.SaveChangesAsync();
        Check((await data.GetMonthlyOverviewAsync(may, 1.5m)).Groups.Single(x => x.Period?.Id == 1).PreviousSummary is null, "Partial-month report is not compared to full previous month");
        (await db.ReportImports.FindAsync(1L))!.IsActive = false;
        await db.SaveChangesAsync();
        try { await data.GetSnapshotAsync(1); Check(false, "Inactive cached report denied"); } catch (InvalidOperationException) { Check(true, "Inactive cached report denied"); }
        (await db.ReportImports.FindAsync(2L))!.PeriodType = ReportPeriodType.Cumulative;
        (await db.ReportImports.FindAsync(2L))!.StartDate = new DateOnly(2026, 1, 1);
        await db.SaveChangesAsync();
        cache.Clear();
        var cumulativeHtml = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<Home>()).ToHtmlString());
        var cumulativeText = System.Net.WebUtility.HtmlDecode(cumulativeHtml);
        Check(cumulativeText.Contains("Yalnızca kümülatif rapor var") && cumulativeText.Contains("Aylık rapor bekleniyor") && !cumulativeText.Contains("10.000,00"), "Cumulative or previous-month amounts never fill missing monthly groups");
        Console.WriteLine($"{checks} checks passed. Real database was not accessed.");
    }

    private static async Task AddScope(TestFactory factory, int id, DateOnly end, string signature, string name)
    {
        await using var db = factory.CreateDbContext();
        var period = new ReportPeriodEntity { Id = id, EndDate = end, CategorySignature = signature };
        var import = new ReportImportEntity { Id = id, ReportPeriod = period, EndDate = end, StartDate = new DateOnly(end.Year, end.Month, 1),
            OriginalFileName = name + ".xls", FileHash = signature, IsActive = true, PeriodType = ReportPeriodType.Monthly };
        db.ReportImports.Add(import);
        db.ReportRows.Add(new ReportRowEntity { Id = id * 1000, ReportImport = import, RowType = ReportRowType.General, SourceReportType = "Örnek", SourceRowNumber = 1, WasteAmount = -999999m, WasteRate = -1m });
        db.ReportRows.Add(new ReportRowEntity { Id = id * 1000 + 1, ReportImport = import, RowType = ReportRowType.CategorySummary, SourceReportType = "Örnek", SourceRowNumber = 2, CategoryCode = signature, CategoryName = name });
        await db.SaveChangesAsync();
    }

    private static async Task Seed(TestFactory factory)
    {
        await using var db = factory.CreateDbContext();
        long rowId = 0;
        for (var periodId = 1; periodId <= 3; periodId++)
        {
            var period = new ReportPeriodEntity { Id = periodId, CategorySignature = periodId == 2 ? "B" : "A", EndDate = new DateOnly(2026, periodId == 3 ? 4 : 5, periodId == 3 ? 30 : 31) };
            var import = new ReportImportEntity { Id = periodId, ReportPeriod = period, ReportPeriodId = periodId, IsActive = true, PeriodType = ReportPeriodType.Monthly, StartDate = new DateOnly(2026, period.EndDate.Month, 1), EndDate = period.EndDate, OriginalFileName = "Mayıs örnek rapor.xls", FileHash = periodId.ToString() };
            db.ReportImports.Add(import);
            ReportRowEntity Row(ReportRowType type, int? store = null) => new() { Id = ++rowId, ReportImport = import, ReportImportId = periodId, RowType = type, SourceRowNumber = (int)rowId, SourceReportType = "Örnek", StoreNumber = store, StoreName = store.HasValue ? $"Mağaza {store:00}" : null, CategoryCode = period.CategorySignature, CategoryName = periodId == 2 ? "Manav" : "Şarküteri", WasteRate = store.HasValue ? -3m : -2m, WasteAmount = store.HasValue ? -100m - store : -10000m, ProfitRate = -5m, ProfitAmount = -500m, StoreSalesAmount = 100000m };
            db.ReportRows.Add(Row(ReportRowType.General));
            db.ReportRows.Add(Row(ReportRowType.CategorySummary));
            for (var store = 1; store <= (periodId == 1 ? 65 : 1); store++)
            {
                var summary = Row(ReportRowType.StoreSummary, store);
                db.ReportRows.Add(summary);
                db.ReportRows.Add(Row(ReportRowType.StoreCategory, store));
                if (periodId != 1) continue;
                db.Stores.Add(new StoreEntity { Id = store, Name = summary.StoreName!, IsExcuseEligible = store != 2 });
                db.ExcuseRequests.Add(new ExcuseRequestEntity { Id = store, ReportRow = summary, ReportRowId = summary.Id, Title = summary.StoreName!, Status = store == 65 ? ExcuseStatus.Answered : ExcuseStatus.Open });
            }
            if (periodId == 1)
            {
                var product = Row(ReportRowType.StoreProduct, 1); product.StockCode = "SON-URUN"; product.StockName = "Örnek peynir";
                db.ReportRows.Add(product);
                db.ExcuseRequests.Add(new ExcuseRequestEntity { Id = 66, ReportRow = product, ReportRowId = product.Id, Title = "Ürün açıklaması", Status = ExcuseStatus.Open });
            }
        }
        await db.SaveChangesAsync();
    }
}
public sealed class TestFactory : IDbContextFactory<AppDbContext>
{
    private readonly DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
    public AppDbContext CreateDbContext() => new(options);
    public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
}
public sealed class TestState : AuthenticationStateProvider
{
    public ClaimsPrincipal User { get; set; } = new(new ClaimsIdentity());
    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(User));
}
public sealed class TestNavigation : NavigationManager
{
    public TestNavigation() => Initialize("http://localhost/", "http://localhost/");
    protected override void NavigateToCore(string uri, bool forceLoad) { }
}
