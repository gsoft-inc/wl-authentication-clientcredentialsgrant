namespace Workleap.Extensions.Http.Authentication.ClientCredentialsGrant;

internal interface IOpenIdConfigurationRetriever
{
    Task<OpenIdConfiguration> GetAsync(string authority, CancellationToken cancellationToken);
}