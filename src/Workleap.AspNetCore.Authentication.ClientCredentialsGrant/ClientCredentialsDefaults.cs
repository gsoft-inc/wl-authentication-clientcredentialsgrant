namespace Workleap.AspNetCore.Authentication.ClientCredentialsGrant;

public static class ClientCredentialsDefaults
{
    public const string AuthenticationScheme = "ClientCredentials";

    internal const string AuthenticationType = "ClientCredentials";

    internal const string RequireClientCredentialsPolicyName = "ClientCredentialsPolicy";

    internal const string AuthorizationReadPolicy = "ClientCredentialsRead";

    internal const string AuthorizationWritePolicy = "ClientCredentialsWrite";

    internal const string AuthorizationAdminPolicy = "ClientCredentialsAdmin";

    internal const string OpenApiSecurityDefinitionId = "clientcredentials";
}