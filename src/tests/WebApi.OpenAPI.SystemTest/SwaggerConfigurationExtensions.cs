using Microsoft.OpenApi.Models;

namespace WebApi.OpenAPI.SystemTest;

public static class SwaggerConfigurationExtensions
{
    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        // Required to detect Minimal Api Endpoints
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Test API", Version = "v1" });
            options.EnableAnnotations();
        });

        return services;
    }
}