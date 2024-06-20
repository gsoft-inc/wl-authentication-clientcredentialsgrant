using Workleap.AspNetCore.Authentication.ClientCredentialsGrant;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Workleap.AspNetCore.Authentication.ClientCredentialsGrant.Swagger;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class AuthenticationBuilderExtensions
{
    private const string AuthenticationSchemesKey = "Schemes";
    private const string AuthenticationConfigKey = "Authentication";

    public static AuthenticationBuilder AddClientCredentials(this AuthenticationBuilder builder)
        => builder.AddClientCredentials(ClientCredentialsDefaults.AuthenticationScheme, _ => { });

    public static AuthenticationBuilder AddClientCredentials(this AuthenticationBuilder builder, Action<JwtBearerOptions> configureOptions)
        => builder.AddClientCredentials(ClientCredentialsDefaults.AuthenticationScheme, configureOptions);

    public static AuthenticationBuilder AddClientCredentials(this AuthenticationBuilder builder, string authScheme, Action<JwtBearerOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOptions<JwtBearerOptions>(authScheme).Configure<IConfiguration>((options, configuration) =>
        {
            var configSection = configuration.GetSection($"{AuthenticationConfigKey}:{AuthenticationSchemesKey}:{authScheme}");
            if (configSection is null || !configSection.GetChildren().Any())
            {
                return;
            }

            configSection.Bind(options);
        });

        builder.AddJwtBearer(authScheme, configureOptions);

        var areNamedSingletonsAlreadyRegistered = builder.Services.Any(x => IsNamedJwtBearerOptionsValidatorAlreadyRegistered(x, authScheme));
        if (!areNamedSingletonsAlreadyRegistered)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<JwtBearerOptions>>(new JwtBearerOptionsValidator(authScheme)));
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<JwtBearerOptions>>(new ClientCredentialsPostConfigureOptions(authScheme)));
        }
        
        builder.Services.ConfigureSwaggerGen(options =>
        {
            if (options.OperationFilterDescriptors.All(x => x.Type != typeof(SecurityRequirementOperationFilter)))
            {
                options.OperationFilter<SecurityRequirementOperationFilter>();
            }
            
            if (options.OperationFilterDescriptors.All(x => x.Type != typeof(SecurityDefinitionDocumentFilter)))
            {
                options.DocumentFilter<SecurityDefinitionDocumentFilter>();
            }
        });

        return builder;
    }

    private static bool IsNamedJwtBearerOptionsValidatorAlreadyRegistered(ServiceDescriptor serviceDescriptor, string name)
    {
        return serviceDescriptor.ServiceType == typeof(IValidateOptions<JwtBearerOptions>)
               && serviceDescriptor.ImplementationInstance is JwtBearerOptionsValidator configureOptions
               && configureOptions.AuthScheme == name;
    }
}