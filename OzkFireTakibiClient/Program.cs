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

// ============================================================================
// Uygulama Başlangıç ve Yapılandırma Dosyası (Program.cs)
// Blazor Server servisleri, SQL Server veritabanı, kimlik doğrulama,
// yetkilendirme politikaları ve middleware ardışık düzeni burada yapılandırılır.
// ============================================================================

var builder = WebApplication.CreateBuilder(args);

// Excel dosyalarının doğru karakter seti (özellikle Türkçe Windows-1254) ile okunabilmesi için encoding sağlayıcısını kaydet
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// --- Servis Kayıtları ---

// Etkileşimli Blazor Server bileşenlerini kaydet
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Her Blazor işleminin kendi SQL Server bağlamını güvenle oluşturabilmesi için DbContext Factory kaydı
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Temel iş mantığı ve yardımcı servisleri (parser, import servisi vb.) DI konteynerine kaydet
builder.Services.AddBaseServices();

// Veri koruma (Data Protection) yapılandırması
builder.Services.AddDataProtection()
    .SetApplicationName("OzkFireTakibi");

// Rapor yükleme yapılandırma seçeneklerini appsettings.json'dan bağla
builder.Services.Configure<ReportImportOptions>(builder.Configuration.GetSection(ReportImportOptions.SectionName));
builder.Services.Configure<ExcuseOptions>(builder.Configuration.GetSection(ExcuseOptions.SectionName));

// Rapor yönetimi için rol bazlı yetkilendirme ilkelerini tanımla
builder.Services.AddAuthorizationCore(options =>
{
    // Rapor yükleme: Admin ve Moderator rolleri yapabilir
    options.AddPolicy(ReportPolicies.CanImportReports, policy =>
        policy.RequireRole(UserRole.Admin.ToString(), UserRole.Moderator.ToString()));

    // Rapor silme: Sadece Admin rolü yapabilir
    options.AddPolicy(ReportPolicies.CanDeleteReports, policy =>
        policy.RequireRole(UserRole.Admin.ToString()));

    // Mazeret değerlendirme: Admin ve Moderator rolleri yapabilir
    options.AddPolicy(ReportPolicies.CanReviewExcuses, policy =>
        policy.RequireRole(UserRole.Admin.ToString(), UserRole.Moderator.ToString()));

    // Aktif aylık raporun alt detayları için mazeret isteme: merkez rollerinin tamamı
    options.AddPolicy(ReportPolicies.CanRequestExcuses, policy =>
        policy.RequireRole(
            UserRole.Admin.ToString(),
            UserRole.Moderator.ToString(),
            UserRole.Observer.ToString()));

    // Mazeret kapsamındaki mağazaları yönetme: Sadece Admin rolü yapabilir
    options.AddPolicy(ReportPolicies.CanManageExcuseStores, policy =>
        policy.RequireRole(UserRole.Admin.ToString()));
});

// Kimlik doğrulama durumunu alt bileşenlere basamaklı (cascading) olarak aktar
builder.Services.AddCascadingAuthenticationState();

// Güvenli tarayıcı yerel ve oturum depolama servisleri
builder.Services.AddScoped<ProtectedLocalStorage>();
builder.Services.AddScoped<ProtectedSessionStorage>();

// Özel kimlik doğrulama sağlayıcısı (CustomStateProvider) kaydı
builder.Services.AddScoped<AuthenticationStateProvider, CustomStateProvider>();
builder.Services.AddScoped(sp => (CustomStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());


var app = builder.Build();

await DbSeeder.SeedAsync(app.Services);

// --- HTTP İstek Ardışık Düzeni (Middleware Pipeline) Yapılandırması ---

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // Üretim ortamı için varsayılan HSTS değeri
    app.UseHsts();
}

// 404 ve diğer durum kodlarını özel hata sayfasına yönlendir
app.UseStatusCodePagesWithReExecute("/notfound", createScopeForStatusCodePages: true);
//app.UseHttpsRedirection();

// CSRF / Antiforgery koruması
app.UseAntiforgery();

// Statik dosyaları ve Blazor kök bileşenini bağla
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
