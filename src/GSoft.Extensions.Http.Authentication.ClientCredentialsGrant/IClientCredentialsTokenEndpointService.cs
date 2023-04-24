namespace GSoft.Extensions.Http.Authentication.ClientCredentialsGrant;

internal interface IClientCredentialsTokenEndpointService
{
    Task<ClientCredentialsToken> RequestTokenAsync(string clientName, CancellationToken cancellationToken);
}