using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Workleap.AspNetCore.Authentication.ClientCredentialsGrant;

public class RequireClientCredentialsRequirementHandler: AuthorizationHandler<RequireClientCredentialsRequirement>
{
    private readonly ScopeFormat _scopeFormat;

    private static readonly HashSet<string> ScopeClaimTypes = new(StringComparer.Ordinal)
    {
        // Claim type currently used by our products
        "scope",

        // However, we should use "scp" instead. "scp" is more common than "scope", as well as being shorter.
        // We put it here in case we ever do the switch.
        "scp",

        // "scp" is by default mapped to this claim type in ASP.NET Core
        // https://github.com/dotnet/dotnet/blob/v8.0.0/src/source-build-externals/src/azure-activedirectory-identitymodel-extensions-for-dotnet/src/Microsoft.IdentityModel.JsonWebTokens/ClaimTypeMapping.cs#L27
        "http://schemas.microsoft.com/identity/claims/scope",
    };

    private readonly JwtBearerOptions _jwtOptions;

    public RequireClientCredentialsRequirementHandler(IOptionsMonitor<JwtBearerOptions> jwtOptionsMonitor)
    {
        // TODO: Scope format to Options
        this._scopeFormat = ScopeFormat.Generic;
        this._jwtOptions = jwtOptionsMonitor.Get(ClientCredentialsDefaults.AuthenticationScheme);
    }
    
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RequireClientCredentialsRequirement requirement)
    {
        if (!this.TryGetRequiredScopes(context, out var requiredScopes))
        {
            return Task.CompletedTask;    
        }
        
        var hasRequiredPermissions = HasOneOfScope(context.User, requiredScopes);

        if (hasRequiredPermissions)
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

        // Why blog proposed interface?
        var requiredPermissions = endpoint?.Metadata.GetMetadata<RequireClientCredentialsAttribute>()?.RequiredPermissions;

        if (requiredPermissions == null)
        {
            return false;
        }

        requiredScopes = requiredPermissions.Select(this.FormatScope).ToArray();
        return true;
    }

    private string FormatScope(string requiredPermission)
    {
        switch (this._scopeFormat)
        {
            case ScopeFormat.Generic:
                return requiredPermission;
            case ScopeFormat.FusionAuth:
                return $"{this._jwtOptions.Audience}:{requiredPermission}";
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private static bool HasOneOfScope(ClaimsPrincipal claimsPrincipal, string[] requiredPermissions)
    {
        return claimsPrincipal.Claims
            .Where(claim => ScopeClaimTypes.Contains(claim.Type)) // TODO: Is it safe or should it be configured?
            .Any(claim => requiredPermissions.Contains(claim.Value, StringComparer.Ordinal)); // TODO: Should we also test if Value contains of the requiredPermissions (in the case it is a list of scopes)?
    }
}