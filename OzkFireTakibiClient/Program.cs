using System.Text;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using OzkFireTakibiClient.Src.Components;
using OzkFireTakibiClient.Src.Data;
using OzkFireTakibiClient.Src.Data.Entities;
using OzkFireTakibiClient.Src;
using OzkFireTakibiClient.Src.Services;
using OzkFireTakibiClient.Src.Authorization;
using OzkFireTakibiClient.Src.Options;

var builder = WebApplication.CreateBuilder(args);

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddBaseServices();

builder.Services.AddDataProtection()
    .SetApplicationName("OzkFireTakibi");

builder.Services.Configure<ReportImportOptions>(builder.Configuration.GetSection(ReportImportOptions.SectionName));
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy(ReportPolicies.CanImportReports, policy =>
        policy.RequireRole(UserRole.Admin.ToString(), UserRole.Moderator.ToString()));
    options.AddPolicy(ReportPolicies.CanDeleteReports, policy =>
        policy.RequireRole(UserRole.Admin.ToString()));
});
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<ProtectedLocalStorage>();
builder.Services.AddScoped<ProtectedSessionStorage>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomStateProvider>();
builder.Services.AddScoped(sp => (CustomStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/notfound", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
