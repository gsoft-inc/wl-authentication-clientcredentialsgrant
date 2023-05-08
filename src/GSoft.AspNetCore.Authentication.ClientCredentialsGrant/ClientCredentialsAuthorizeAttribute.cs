using GSoft.AspNetCore.Authentication.ClientCredentialsGrant;

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
        this.Scope = scope;
        this.Policy = this._policyScopeMapping.GetValueOrDefault(scope);
    }

    public ClientCredentialsScope Scope { get; }
}