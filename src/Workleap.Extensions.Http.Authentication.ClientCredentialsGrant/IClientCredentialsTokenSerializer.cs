namespace Workleap.Extensions.Http.Authentication.ClientCredentialsGrant;

internal interface IClientCredentialsTokenSerializer
{
    byte[] Serialize(string clientName, ClientCredentialsToken token);

    ClientCredentialsToken Deserialize(string clientName, byte[] bytes);
}