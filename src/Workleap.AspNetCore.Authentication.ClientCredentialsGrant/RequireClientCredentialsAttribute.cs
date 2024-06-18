using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;

namespace Workleap.AspNetCore.Authentication.ClientCredentialsGrant;

// TODO: Permission or Scope (or RequireClientCredentials)
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireClientCredentialsAttribute: AuthorizeAttribute
{
    private static readonly Dictionary<ClientCredentialsScope, string> EnumScopeNameMapping = new()
    {
        [ClientCredentialsScope.Read] = "read",
        [ClientCredentialsScope.Write] = "write",
        [ClientCredentialsScope.Admin] = "admin",
    };

    public RequireClientCredentialsAttribute(ClientCredentialsScope scope)
        : this(EnumScopeNameMapping.GetValueOrDefault(scope) ?? throw new ArgumentException($"{scope} is not an valid scope value"))
    {
    }
    
    public RequireClientCredentialsAttribute(string requiredPermission, params string[] additionalRequiredPermissions)
    {
        this.Policy = ClientCredentialsDefaults.ClientCredentialRequirePermissions;
        this.RequiredPermissions = [requiredPermission, ..additionalRequiredPermissions];
    }

    public HashSet<string> RequiredPermissions { get; set; }
}

// For minimal API
public static class RequiredScoeAttributeExtensions
{
    public static TBuilder RequirePermission<TBuilder>(
        this TBuilder endpointConventionBuilder, string requiredPermission, params string[] additionalRequiredPermissions)
        where TBuilder : IEndpointConventionBuilder
    {
        return endpointConventionBuilder.WithMetadata(new RequireClientCredentialsAttribute(requiredPermission, additionalRequiredPermissions));
    }

}
