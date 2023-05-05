using GSoft.AspNetCore.Authentication.ClientCredentialsGrant;
using GSoft.AspNetCore.Authentication.ClientCredentialsGrant.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class AuthorizationExtensions
{
    public static void AddClientCredentialsAuthorization(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddAuthorization();

        services.AddOptions<AuthorizationOptions>()
            .Configure<IOptionsMonitor<JwtBearerOptions>>((authorizationOptions, jwtOptionsMonitor) =>
            {
                var jwtOptions = jwtOptionsMonitor.Get(ClientCredentialsDefaults.AuthenticationScheme);

                authorizationOptions.AddPolicy(
                    ClientCredentialsDefaults.AuthorizationReadPolicy,
                    policy => policy
                        .AddAuthenticationSchemes(ClientCredentialsDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser()
                        .RequireClaim("scope", $"{jwtOptions.Audience}:{ClientCredentialsScope.Read}"));

                authorizationOptions.AddPolicy(
                    ClientCredentialsDefaults.AuthorizationWritePolicy,
                    policy => policy
                        .AddAuthenticationSchemes(ClientCredentialsDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser()
                        .RequireClaim("scope", $"{jwtOptions.Audience}:{ClientCredentialsScope.Write}"));

                authorizationOptions.AddPolicy(
                    ClientCredentialsDefaults.AuthorizationAdminPolicy,
                    policy => policy
                        .AddAuthenticationSchemes(ClientCredentialsDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser()
                        .RequireClaim("scope", $"{jwtOptions.Audience}:{ClientCredentialsScope.Admin}"));
            });
    }
}