using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Workleap.AspNetCore.Authentication.ClientCredentialsGrant.OpenAPI;

/// <summary>
/// Extract all permissions defined on this API based on the <see cref="RequireClientCredentialsAttribute"/> attributes and set it as security definition in OpenAPI.
/// </summary>
internal sealed class SecurityDefinitionDocumentFilter(IOptionsMonitor<JwtBearerOptions> jwtOptionsMonitor) : IDocumentFilter
{
    private readonly JwtBearerOptions _jwtOptions = jwtOptionsMonitor.Get(ClientCredentialsDefaults.AuthenticationScheme);

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var apiPermissions = context.ApiDescriptions.SelectMany(SwaggerUtils.GetRequiredPermissions).ToHashSet(StringComparer.Ordinal);

        swaggerDoc.Components.SecuritySchemes.Add(
            ClientCredentialsDefaults.OpenApiSecurityDefinitionId,
            new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    ClientCredentials = new OpenApiOAuthFlow
                    {
                        TokenUrl = this.GetTokenUrl(),
                        Scopes = this.ExtractScopes(apiPermissions),
                    },
                },
            });
    }

    private Uri GetTokenUrl()
    {
        // Authority has already been validated as an absolute URL
        var authority = this._jwtOptions.Authority!.TrimEnd('/');
        return new Uri($"{authority}/oauth2/token", UriKind.Absolute);
    }

    private Dictionary<string, string> ExtractScopes(IEnumerable<string> permissions)
    {
        // Audience has already been validated as non-empty
        var audience = this._jwtOptions.Audience!;

        var scopes = new Dictionary<string, string>
        {
            [SwaggerUtils.GetScopeForAnyPermission(audience)] = "Request all permissions for specified client ID",
        };

        foreach (var permission in permissions)
        {
            scopes[SwaggerUtils.FormatScopeForSpecificPermission(audience, permission)] = $"Request permission '{permission}' for specified client ID";
        }

        return scopes;
    }
}