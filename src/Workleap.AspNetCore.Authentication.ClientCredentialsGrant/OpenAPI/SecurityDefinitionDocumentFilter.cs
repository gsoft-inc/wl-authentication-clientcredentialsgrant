using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Workleap.AspNetCore.Authentication.ClientCredentialsGrant.OpenAPI;

/// <summary>
/// Extract all permissions defined on this API based on the <see cref="RequireClientCredentialsAttribute"/> attributes and set it as security definition in OpenAPI.
/// </summary>
internal sealed class SecurityDefinitionDocumentFilter : IDocumentFilter
{
    private readonly JwtBearerOptions _jwtOptions;

    public SecurityDefinitionDocumentFilter(IOptionsMonitor<JwtBearerOptions> jwtOptionsMonitor)
    {
        this._jwtOptions = jwtOptionsMonitor.Get(ClientCredentialsDefaults.AuthenticationScheme);
    }
    
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var apiPermissions = context.ApiDescriptions.SelectMany(SwaggerUtils.GetRequiredPermissions).Distinct();
        
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
        var authority = this._jwtOptions.Authority?.TrimEnd('/');
        return new Uri($"{authority}/oauth2/token", UriKind.RelativeOrAbsolute);
    }
    
    private Dictionary<string, string> ExtractScopes(IEnumerable<string> permissions)
    {
        var audience = this._jwtOptions.Audience ?? string.Empty;
        
        var scopes = new Dictionary<string, string>
        {
            { SwaggerUtils.GetAllScope(audience), "Request all permissions for specified client_id" },
        };

        foreach (var permission in permissions)
        {
            scopes[SwaggerUtils.FormatScope(audience, $"Request this permission {permission} for specified client_id")] = permission;
        }

        return scopes;
    }
}