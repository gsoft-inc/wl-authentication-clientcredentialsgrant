using Workleap.AspNetCore.Authentication.ClientCredentialsGrant;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;


// public async Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, 
//     object resource, IEnumerable<IAuthorizationRequirement> requirements)
// {
//     // Create a tracking context from the authorization inputs.
//     var authContext = _contextFactory.CreateContext(requirements, user, resource);
//
//     // By default this returns an IEnumerable<IAuthorizationHandler> from DI.
//     var handlers = await _handlers.GetHandlersAsync(authContext);
//
//     // Invoke all handlers.
//     foreach (var handler in handlers)
//     {
//         await handler.HandleAsync(authContext);
//     }
//
//     // Check the context, by default success is when all requirements have been met.
//     return _evaluator.Evaluate(authContext);
// }

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

                // Require Claim and Roles are only shortcuts to add requirements
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
        
        // Add pr TryAddEnumerable?
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, RequireClientCredentialsRequirementHandler>());

        return services;
    }
}