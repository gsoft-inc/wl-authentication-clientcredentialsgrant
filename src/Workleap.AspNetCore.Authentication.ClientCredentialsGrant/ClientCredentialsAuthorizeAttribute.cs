using Workleap.AspNetCore.Authentication.ClientCredentialsGrant;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Authorization;

public sealed class ClientCredentialsAuthorizeAttribute : AuthorizeAttribute
{
    private readonly Dictionary<ClientCredentialsScope, string> _policyScopeMapping = new()
    {
        [ClientCredentialsScope.Read] = ClientCredentialsDefaults.AuthorizationReadPolicy,
        [ClientCredentialsScope.Write] = ClientCredentialsDefaults.AuthorizationWritePolicy,
        [ClientCredentialsScope.Admin] = ClientCredentialsDefaults.AuthorizationAdminPolicy,
    };

    public ClientCredentialsAuthorizeAttribute(ClientCredentialsScope scope)
    {
        if (!this._policyScopeMapping.TryGetValue(scope, out var policy))
        {
            throw new ArgumentException($"${scope} is not an valid scope value");
        }

        this.Scope = scope;
        this.Policy = policy;
    }

    public ClientCredentialsScope Scope { get; }
}