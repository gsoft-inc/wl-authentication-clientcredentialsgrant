using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Workleap.AspNetCore.Authentication.ClientCredentialsGrant.Swagger;

internal sealed class SecurityRequirementOperationFilter : IOperationFilter
{
    private readonly string? _audience;

    public SecurityRequirementOperationFilter(IOptionsMonitor<JwtBearerOptions> jwtOptionsMonitor)
    {
        var jwtOptions = jwtOptionsMonitor.Get(ClientCredentialsDefaults.AuthenticationScheme);
        this._audience = jwtOptions.Audience;
    }
    
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var attributes = SwaggerUtils.GetRequiredPermissions(context.ApiDescription).ToArray();
        if (attributes.Length == 0)
        {
            return;
        }

        // Method need to be idempotent since minimal api are preserving the state
        AddAuthzErrorResponse(operation);
        this.AddOperationSecurityReference(operation, attributes);
        AppendScopeToOperationSummary(operation, attributes);
    }
    
    private static void AddAuthzErrorResponse(OpenApiOperation operation)
    {
        operation.Responses.TryAdd(StatusCodes.Status401Unauthorized.ToString(CultureInfo.InvariantCulture), new OpenApiResponse { Description = ReasonPhrases.GetReasonPhrase(StatusCodes.Status401Unauthorized) });
        operation.Responses.TryAdd(StatusCodes.Status403Forbidden.ToString(CultureInfo.InvariantCulture), new OpenApiResponse { Description = ReasonPhrases.GetReasonPhrase(StatusCodes.Status403Forbidden) });
    }
    
    private void AddOperationSecurityReference(OpenApiOperation operation, string[] permissions)
    {
        if (operation.Security.Any(requirement => requirement.Keys.Any(key => key.Reference?.Id == ClientCredentialsDefaults.OpenApiSecurityDefinitionId)))
        {
            return;
        }
        
        var securityScheme = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = ClientCredentialsDefaults.OpenApiSecurityDefinitionId,
            },
        };

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [securityScheme] = this.ExtractScopes(permissions).ToList(),
        });
    }

    private static void AppendScopeToOperationSummary(OpenApiOperation operation, string[] scopes)
    {
        var requireScopeSummary = new StringBuilder();
        requireScopeSummary.Append(scopes.Length == 1 ? "Required scope: " : "Required scopes: ");
        requireScopeSummary.Append(string.Join(", ", scopes));
        requireScopeSummary.Append('.');
        
        var isRequireScopeSummaryPresent = operation.Summary?.Contains(requireScopeSummary.ToString()) ?? false;
        if (isRequireScopeSummaryPresent)
        {
            return;
        }
        
        var summary = new StringBuilder(operation.Summary?.TrimEnd('.'));
        if (summary.Length > 0)
        {
            summary.Append(". ");
        }
        
        summary.Append(requireScopeSummary);

        operation.Summary = summary.ToString();
    }
    
    private IEnumerable<string> ExtractScopes(string[] permissions)
    {
        foreach (var permission in permissions)
        {
            yield return $"target-entity:{this._audience}:{permission}";
        }
    }
}