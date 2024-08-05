using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;

namespace Workleap.AspNetCore.Authentication.ClientCredentialsGrant;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequireClientCredentialsAttribute : AuthorizeAttribute
{
    private static readonly Dictionary<ClientCredentialsScope, string> EnumScopeNameMapping = new()
    {
        [ClientCredentialsScope.Read] = "read",
        [ClientCredentialsScope.Write] = "write",
        [ClientCredentialsScope.Admin] = "admin",
    };

    /// <summary>
    /// Verifies that the endpoint is called with the right classic scope. <br/>
    /// - It will check in those claims type: scope, scp or http://schemas.microsoft.com/identity/claims/scope <br/>
    /// - It will accept those value format: `read`, `{Audience}:read`
    /// </summary>
    /// <remarks>
    /// Used when using the classic scope Read, Write, Admin.
    /// </remarks>
    /// <example>
    /// <code>
    /// [RequireClientCredentials(ClientCredentialsScope.Read)]
    /// </code>
    /// </example>
    [SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments", Justification = "The arguments are transformed.")]
    public RequireClientCredentialsAttribute(ClientCredentialsScope scope)
        : this(EnumScopeNameMapping.GetValueOrDefault(scope) ?? throw new ArgumentOutOfRangeException(nameof(scope), scope, $"'{scope}' is not an valid scope value"))
    {
    }

    /// <summary>
    /// Verifies that the endpoint is called with the right granular permissions. <br/>
    /// - It will check in those claims type: scope, scp or http://schemas.microsoft.com/identity/claims/scope <br/>
    /// - It will accept those value format: `read`, `{Audience}:read`
    /// </summary>
    /// <param name="requiredPermission">The required permission expected to be in the scope claims.</param>
    /// <example>
    /// <code>
    /// [RequireClientCredentials("invoices.read")]
    /// </code>
    /// </example>
    public RequireClientCredentialsAttribute(string requiredPermission)
    {
        this.Policy = ClientCredentialsDefaults.RequireClientCredentialsPolicyName;
        this.RequiredPermission = requiredPermission ?? throw new ArgumentNullException(nameof(requiredPermission));
    }

    public string RequiredPermission { get; }
}

public static class RequireClientCredentialsExtensions
{
    /// <summary>
    /// Verifies that the endpoint is called with the right granular permissions. <br/>
    /// - It will check in those claims type: scope, scp or http://schemas.microsoft.com/identity/claims/scope <br/>
    /// - It will accept those value format: `read`, `{Audience}:read`
    /// </summary>
    /// <remarks>
    /// Used in minimal APIs.
    /// </remarks>
    /// <example>
    /// <param name="requiredPermission">The required permission expected to be in the claims.</param>
    /// <code>
    /// app.MapGet("/weather", () => { /* ... */ }).RequireClientCredentials("read");
    /// </code>
    /// </example>
    public static TBuilder RequireClientCredentials<TBuilder>(
        this TBuilder endpointConventionBuilder, string requiredPermission)
        where TBuilder : IEndpointConventionBuilder
    {
        return endpointConventionBuilder.WithMetadata(new RequireClientCredentialsAttribute(requiredPermission));
    }
}
