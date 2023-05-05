using GSoft.AspNetCore.Authentication.ClientCredentialsGrant.Enums;
using Microsoft.AspNetCore.Authorization;

namespace GSoft.AspNetCore.Authentication.ClientCredentialsGrant;

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
        this.Policy = _policyScopeMapping.GetValueOrDefault(scope);
    }
}