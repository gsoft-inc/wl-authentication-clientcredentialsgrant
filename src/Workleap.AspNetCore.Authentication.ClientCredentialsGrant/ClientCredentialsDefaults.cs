namespace Workleap.AspNetCore.Authentication.ClientCredentialsGrant;

public static class ClientCredentialsDefaults
{
    public const string AuthenticationScheme = "ClientCredentials";

    internal const string AuthenticationType = "ClientCredentials";
    
    internal const string AuthorizationRequirePermissionsPolicy = "ClientCredentialRequirePermissions";

    internal const string AuthorizationReadPolicy = "ClientCredentialsRead";

    internal const string AuthorizationWritePolicy = "ClientCredentialsWrite";

    internal const string AuthorizationAdminPolicy = "ClientCredentialsAdmin";

    internal static readonly string OpenApiSecurityDefinitionId = AuthenticationScheme.ToLowerInvariant();
}