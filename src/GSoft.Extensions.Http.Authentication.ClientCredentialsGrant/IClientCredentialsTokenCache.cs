namespace GSoft.Extensions.Http.Authentication.ClientCredentialsGrant;

internal interface IClientCredentialsTokenCache
{
    Task SetAsync(string clientName, ClientCredentialsToken token, CancellationToken cancellationToken);

    Task<ClientCredentialsToken?> GetAsync(string clientName, CancellationToken cancellationToken);
}