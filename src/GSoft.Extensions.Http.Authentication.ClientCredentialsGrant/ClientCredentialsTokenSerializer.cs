using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace GSoft.Extensions.Http.Authentication.ClientCredentialsGrant;

/// <summary>
/// Class responsible for serializing and deserializing <see cref="ClientCredentialsToken"/> to/from bytes.
/// </summary>
internal sealed class ClientCredentialsTokenSerializer : IClientCredentialsTokenSerializer
{
    private readonly IDataProtector _dataProtector;

    public ClientCredentialsTokenSerializer(IDataProtectionProvider dataProtectionProvider)
    {
        this._dataProtector = dataProtectionProvider.CreateProtector(ClientCredentialsConstants.DataProtectionPurpose);
    }

    public byte[] Serialize(string clientName, ClientCredentialsToken token)
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(token);
            return this._dataProtector.Protect(bytes);
        }
        catch (Exception ex)
        {
            throw new ClientCredentialsException($"An error occured while serializing token for client '{clientName}'", ex);
        }
    }

    public ClientCredentialsToken Deserialize(string clientName, byte[] bytes)
    {
        ClientCredentialsToken? token;

        try
        {
            bytes = this._dataProtector.Unprotect(bytes);
            token = JsonSerializer.Deserialize<ClientCredentialsToken>(bytes);
        }
        catch (Exception ex)
        {
            throw new ClientCredentialsException($"An error occured while deserializing token for client '{clientName}'", ex);
        }

        if (token == null)
        {
            throw new ClientCredentialsException($"Deserializing token for client '{clientName}' returned null");
        }

        return token;
    }
}