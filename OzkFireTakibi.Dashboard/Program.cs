using System.Text;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using OzkFireTakibi.Dashboard.Authentication;
using OzkFireTakibi.Dashboard.Components;
using OzkFireTakibi.Dashboard.Services;
using OzkFireTakibi.Dashboard.Authorization;
using OzkFireTakibi.Dashboard.Data;
using OzkFireTakibi.Dashboard.Data.Entities;
using OzkFireTakibi.Dashboard.Options;
using OzkFireTakibi.Dashboard.Importing;

var builder = WebApplication.CreateBuilder(args);

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMemoryCache();
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<ReportDataService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<LoginService>();
builder.Services.AddSingleton<ReportImportParser>();
builder.Services.AddScoped<ReportImportService>();
builder.Services.AddScoped<ExcuseAutomationService>();
builder.Services.AddScoped<ExcuseService>();
builder.Services.Configure<ReportImportOptions>(builder.Configuration.GetSection(ReportImportOptions.SectionName));
builder.Services.Configure<ExcuseOptions>(builder.Configuration.GetSection(ExcuseOptions.SectionName));
builder.Services.AddDataProtection()
    .SetApplicationName("OzkFireTakibi.Dashboard");
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy(ReportPolicies.CanImportReports, policy =>
        policy.RequireRole(UserRole.Admin.ToString(), UserRole.Moderator.ToString()));
    options.AddPolicy(ReportPolicies.CanDeleteReports, policy =>
        policy.RequireRole(UserRole.Admin.ToString()));
    options.AddPolicy(ReportPolicies.CanReviewExcuses, policy =>
        policy.RequireRole(UserRole.Admin.ToString(), UserRole.Moderator.ToString()));
    options.AddPolicy(ReportPolicies.CanRequestExcuses, policy =>
        policy.RequireRole(
            UserRole.Admin.ToString(),
            UserRole.Moderator.ToString(),
            UserRole.Observer.ToString()));
    options.AddPolicy(ReportPolicies.CanManageExcuseStores, policy =>
        policy.RequireRole(UserRole.Admin.ToString()));
});
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<ProtectedLocalStorage>();
builder.Services.AddScoped<ProtectedSessionStorage>();
builder.Services.AddScoped<AuthenticationStateProvider, DashboardAuthenticationStateProvider>();
builder.Services.AddScoped(sp =>
    (DashboardAuthenticationStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
