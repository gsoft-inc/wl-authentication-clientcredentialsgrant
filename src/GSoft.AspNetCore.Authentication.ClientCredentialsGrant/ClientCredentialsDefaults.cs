namespace GSoft.AspNetCore.Authentication.ClientCredentialsGrant;

public sealed class ClientCredentialsDefaults
{
    public const string AuthenticationScheme = "ClientCredentials";

    internal const string AuthorizationReadPolicy = "ClientCredentialsRead";

    internal const string AuthorizationWritePolicy = "ClientCredentialsWrite";

    internal const string AuthorizationAdminPolicy = "ClientCredentialsAdmin";
}