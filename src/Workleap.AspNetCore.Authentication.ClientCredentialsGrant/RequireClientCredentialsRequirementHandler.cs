using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Workleap.AspNetCore.Authentication.ClientCredentialsGrant;

// Inspired from https://github.com/AzureAD/microsoft-identity-web/blob/2.17.0/src/Microsoft.Identity.Web/Policy/ScopeAuthorizationHandler.cs#L38
internal class RequireClientCredentialsRequirementHandler : AuthorizationHandler<RequireClientCredentialsRequirement>
{
    private static readonly HashSet<string> ScopeClaimTypes = new(StringComparer.Ordinal)
    {
        // Claim type currently used by our products
        "scope",

        // However "scp" is more common than "scope", as well as being shorter.
        "scp",

        // "scp" is by default mapped to this claim type in ASP.NET Core
        // https://github.com/dotnet/dotnet/blob/v8.0.0/src/source-build-externals/src/azure-activedirectory-identitymodel-extensions-for-dotnet/src/Microsoft.IdentityModel.JsonWebTokens/ClaimTypeMapping.cs#L27
        "http://schemas.microsoft.com/identity/claims/scope",
    };

    private readonly JwtBearerOptions _jwtOptions;

    public RequireClientCredentialsRequirementHandler(IOptionsMonitor<JwtBearerOptions> jwtOptionsMonitor)
    {
        this._jwtOptions = jwtOptionsMonitor.Get(ClientCredentialsDefaults.AuthenticationScheme);
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RequireClientCredentialsRequirement requirement)
    {
        if (!this.TryGetRequiredScopes(context, out var requiredScopes))
        {
            return Task.CompletedTask;
        }

        var hasRequiredScope = HasRequiredScope(context.User, requiredScopes);
        if (hasRequiredScope)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private bool TryGetRequiredScopes(AuthorizationHandlerContext context, [NotNullWhen(true)] out string[]? requiredScopes)
    {
        requiredScopes = null;

        var endpoint = context.Resource switch
        {
            HttpContext httpContext => httpContext.GetEndpoint(),
            Endpoint ep => ep,
            _ => null,
        };

        var requiredPermission = endpoint?.Metadata.GetMetadata<RequireClientCredentialsAttribute>()?.RequiredPermission;
        if (requiredPermission == null)
        {
            return false;
        }

        requiredScopes = this.FormatScopes(requiredPermission);
        return true;
    }

    private string[] FormatScopes(string requiredPermission)
    {
        return [requiredPermission, $"{this._jwtOptions.Audience}:{requiredPermission}"];
    }

    private static bool HasRequiredScope(ClaimsPrincipal claimsPrincipal, string[] requiredScopes)
    {
        return claimsPrincipal.Claims
            .Where(x => ScopeClaimTypes.Contains(x.Type))
            .Any(x => requiredScopes.Contains(x.Value));
    }
}