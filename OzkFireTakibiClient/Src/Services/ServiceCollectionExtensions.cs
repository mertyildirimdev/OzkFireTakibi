using OzkFireTakibiClient.Src.ReportImports;

namespace OzkFireTakibiClient.Src.Services;

/// <summary>
/// Servis bağımlılıklarının (Dependency Injection) IoC konteynerine kaydı için genişletme metotları.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Rapor ayrıştırma, içe aktarma ve BaseService'ten türeyen tüm iş servislerini DI konteynerine Scoped/Singleton olarak kaydeder.
    /// </summary>
    public static IServiceCollection AddBaseServices(this IServiceCollection services)
    {
        // Rapor ayrıştırıcı durumsuz (stateless) olduğu için Singleton
        services.AddSingleton<ReportImportParser>();

        // Rapor iş mantığı servisi Scoped
        services.AddScoped<ReportImportService>();

        // BaseService soyut sınıfından türeyen tüm somut servis sınıflarını reflection ile dinamik olarak Scoped kaydet
        var serviceTypes = typeof(BaseService).Assembly
            .GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && typeof(BaseService).IsAssignableFrom(type));

        foreach (var serviceType in serviceTypes)
        {
            services.AddScoped(serviceType);
        }

        return services;
    }
}
