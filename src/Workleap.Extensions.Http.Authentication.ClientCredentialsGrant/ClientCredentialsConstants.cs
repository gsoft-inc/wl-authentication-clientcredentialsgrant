namespace Workleap.Extensions.Http.Authentication.ClientCredentialsGrant;

public static class ClientCredentialsConstants
{
    /// <summary>
    /// Name of the HttpClient internally used to communicate with the authority / identity provider,
    /// for instance to retrieve the access tokens or downloading the metadata documents (&lt;authority&gt;/.well-known/openid-configuration).
    /// Use this client name to customize the configured HttpClient or to remove/replace the default retry handler.
    /// </summary>
    public const string BackchannelHttpClientName = "Workleap.Authentication.ClientCredentialsGrant";

    // This data protection purpose string must not change.
    // Any change will break the read of existing serialized/protected tokens:
    // https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/purpose-strings
    internal const string DataProtectionPurpose = "gsoft_authentication_clientcredentialsgrant";
}