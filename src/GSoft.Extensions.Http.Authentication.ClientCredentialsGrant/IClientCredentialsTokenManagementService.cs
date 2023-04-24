namespace GSoft.Extensions.Http.Authentication.ClientCredentialsGrant;

internal interface IClientCredentialsTokenManagementService
{
    Task<ClientCredentialsToken> GetAccessTokenAsync(string clientName, CachingBehavior cachingBehavior, CancellationToken cancellationToken);
}