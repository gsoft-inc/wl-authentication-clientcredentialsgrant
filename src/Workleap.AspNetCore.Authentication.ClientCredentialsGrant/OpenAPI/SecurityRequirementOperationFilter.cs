using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Workleap.AspNetCore.Authentication.ClientCredentialsGrant.OpenAPI;

/// <summary>
/// Add client credential security requirement for each endpoints in OpenAPI based on the <see cref="RequireClientCredentialsAttribute"/> attributes.
/// </summary>
internal sealed class SecurityRequirementOperationFilter(IOptionsMonitor<JwtBearerOptions> jwtOptionsMonitor) : IOperationFilter
{
    private readonly JwtBearerOptions _jwtOptions = jwtOptionsMonitor.Get(ClientCredentialsDefaults.AuthenticationScheme);

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var attributes = SwaggerUtils.GetRequiredPermissions(context.ApiDescription).ToHashSet(StringComparer.Ordinal);
        if (attributes.Count == 0)
        {
            return;
        }

        // Method need to be idempotent since minimal api are preserving the state
        AddAuthenticationAndAuthorizationErrorResponse(operation);
        this.AddOperationSecurityReference(operation, attributes);
        AppendScopeToOperationSummary(operation, attributes);
    }

    private static void AddAuthenticationAndAuthorizationErrorResponse(OpenApiOperation operation)
    {
        operation.Responses.TryAdd(StatusCodes.Status401Unauthorized.ToString(CultureInfo.InvariantCulture), new OpenApiResponse { Description = ReasonPhrases.GetReasonPhrase(StatusCodes.Status401Unauthorized) });
        operation.Responses.TryAdd(StatusCodes.Status403Forbidden.ToString(CultureInfo.InvariantCulture), new OpenApiResponse { Description = ReasonPhrases.GetReasonPhrase(StatusCodes.Status403Forbidden) });
    }

    private void AddOperationSecurityReference(OpenApiOperation operation, HashSet<string> permissions)
    {
        var isAlreadyReferencingSecurityDefinition = operation.Security.Any(requirement => requirement.Keys.Any(key => key.Reference?.Id == ClientCredentialsDefaults.OpenApiSecurityDefinitionId));
        if (isAlreadyReferencingSecurityDefinition)
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

    private static void AppendScopeToOperationSummary(OpenApiOperation operation, HashSet<string> scopes)
    {
        var requireScopeSummary = new StringBuilder();
        requireScopeSummary.Append(scopes.Count == 1 ? "Required permission: " : "Required permissions: ");
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

    private IEnumerable<string> ExtractScopes(HashSet<string> permissions)
    {
        foreach (var permission in permissions)
        {
            yield return SwaggerUtils.FormatScopeForSpecificPermission(this._jwtOptions.Audience!, permission);
        }
    }
}