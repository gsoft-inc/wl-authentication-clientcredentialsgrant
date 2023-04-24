using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace GSoft.Extensions.Http.Authentication.ClientCredentialsGrant;

/// <summary>
/// Class responsible for serializing and deserializing <see cref="ClientCredentialsToken"/> to/from bytes.
/// </summary>
internal sealed class ClientCredentialsTokenProtectedSerializer : IClientCredentialsTokenSerializer
{
    // This purpose string must not change.
    // Changing this data protection purpose string will break the read of existing serialized/protected tokens:
    // https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/purpose-strings
    private const string DefaultPurpose = "gsoft_authentication_clientcredentialsgrant";

    private readonly IDataProtector _dataProtector;

    public ClientCredentialsTokenProtectedSerializer(IDataProtectionProvider dataProtectionProvider)
    {
        this._dataProtector = dataProtectionProvider.CreateProtector(DefaultPurpose);
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