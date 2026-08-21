using OzkFireTakibiClient.Src.ReportImports;

namespace OzkFireTakibiClient.Src.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBaseServices(this IServiceCollection services)
    {
        services.AddSingleton<ReportImportParser>();
        services.AddScoped<ReportImportService>();

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
