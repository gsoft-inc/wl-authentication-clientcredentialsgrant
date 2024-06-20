using Workleap.AspNetCore.Authentication.ClientCredentialsGrant;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class AuthorizationExtensions
{
    internal static readonly Dictionary<ClientCredentialsScope, string> ScopeClaimMapping = new()
    {
        [ClientCredentialsScope.Read] = "read",
        [ClientCredentialsScope.Write] = "write",
        [ClientCredentialsScope.Admin] = "admin",
    };

    public static IServiceCollection AddClientCredentialsAuthorization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthorization();
        services.AddOptions<AuthorizationOptions>()
            .Configure<IOptionsMonitor<JwtBearerOptions>>(static (authorizationOptions, jwtOptionsMonitor) =>
            {
                var jwtOptions = jwtOptionsMonitor.Get(ClientCredentialsDefaults.AuthenticationScheme);

                authorizationOptions.AddPolicy(
                    ClientCredentialsDefaults.AuthorizationReadPolicy,
                    policy => policy
                        .AddAuthenticationSchemes(ClientCredentialsDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser()
                        .RequireClaim("scope", $"{jwtOptions.Audience}:{ScopeClaimMapping[ClientCredentialsScope.Read]}"));

                authorizationOptions.AddPolicy(
                    ClientCredentialsDefaults.AuthorizationWritePolicy,
                    policy => policy
                        .AddAuthenticationSchemes(ClientCredentialsDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser()
                        .RequireClaim("scope", $"{jwtOptions.Audience}:{ScopeClaimMapping[ClientCredentialsScope.Write]}"));

                authorizationOptions.AddPolicy(
                    ClientCredentialsDefaults.AuthorizationAdminPolicy,
                    policy => policy
                        .AddAuthenticationSchemes(ClientCredentialsDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser()
                        .RequireClaim("scope", $"{jwtOptions.Audience}:{ScopeClaimMapping[ClientCredentialsScope.Admin]}"));
                
                authorizationOptions.AddPolicy(
                    ClientCredentialsDefaults.AuthorizationRequirePermissionsPolicy,
                    policy => policy
                        .AddAuthenticationSchemes(ClientCredentialsDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser()
                        .AddRequirements(new RequireClientCredentialsRequirement()));
            });
        
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, RequireClientCredentialsRequirementHandler>());

        return services;
    }
}