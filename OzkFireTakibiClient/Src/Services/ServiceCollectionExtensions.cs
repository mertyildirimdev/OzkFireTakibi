using OzkFireTakibiClient.Src.ReportImports;

namespace OzkFireTakibiClient.Src.Services;

/// <summary>
/// Servis bağımlılıklarının (Dependency Injection) IoC konteynerine kaydı için genişletme metotları.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Rapor ayrıştırma, içe aktarma ve tüm iş servislerini DI konteynerine Scoped/Singleton olarak kaydeder.
    /// </summary>
    public static IServiceCollection AddBaseServices(this IServiceCollection services)
    {
        // Rapor ayrıştırıcı durumsuz (stateless) olduğu için Singleton
        services.AddSingleton<ReportImportParser>();

        // İş mantığı servisleri Scoped
        services.AddScoped<ReportImportService>();
        services.AddScoped<ExcuseAutomationService>();
        services.AddScoped<ExcuseService>();
        services.AddScoped<UserService>();
        services.AddScoped<LoginService>();

        return services;
    }
}
