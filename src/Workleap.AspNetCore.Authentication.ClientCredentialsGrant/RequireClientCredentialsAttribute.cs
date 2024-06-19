using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;

namespace Workleap.AspNetCore.Authentication.ClientCredentialsGrant;

// TODO: Permission or Scope (or RequireClientCredentials)
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireClientCredentialsAttribute : AuthorizeAttribute
{
    private static readonly Dictionary<ClientCredentialsScope, string> EnumScopeNameMapping = new()
    {
        [ClientCredentialsScope.Read] = "read",
        [ClientCredentialsScope.Write] = "write",
        [ClientCredentialsScope.Admin] = "admin",
    };

    [SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments", Justification = "The arguments are transformed.")]
    public RequireClientCredentialsAttribute(ClientCredentialsScope scope)
        : this(EnumScopeNameMapping.GetValueOrDefault(scope) ?? throw new ArgumentException($"{scope} is not an valid scope value"))
    {
    }
    
    [SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments", Justification = "The arguments are transformed.")]
    public RequireClientCredentialsAttribute(string requiredPermission, params string[] additionalRequiredPermissions)
    {
        this.Policy = ClientCredentialsDefaults.AuthorizationRequirePermissionsPolicy;
        this.RequiredPermissions = [requiredPermission, ..additionalRequiredPermissions];
    }

    internal HashSet<string> RequiredPermissions { get; }
}

public static class RequireClientCredentialsExtensions
{
    public static TBuilder RequirePermission<TBuilder>(
        this TBuilder endpointConventionBuilder, string requiredPermission, params string[] additionalRequiredPermissions)
        where TBuilder : IEndpointConventionBuilder
    {
        return endpointConventionBuilder.WithMetadata(new RequireClientCredentialsAttribute(requiredPermission, additionalRequiredPermissions));
    }
}
