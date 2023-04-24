using System.Text.Json.Serialization;

namespace GSoft.Extensions.Http.Authentication.ClientCredentialsGrant;

internal sealed class ClientCredentialsToken
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; init; } = string.Empty;

    [JsonPropertyName("expiration")]
    public DateTimeOffset Expiration { get; init; }

    private bool Equals(ClientCredentialsToken other)
    {
        return this.AccessToken == other.AccessToken && this.Expiration.Equals(other.Expiration);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || (obj is ClientCredentialsToken other && this.Equals(other));
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (this.AccessToken.GetHashCode() * 397) ^ this.Expiration.GetHashCode();
        }
    }
}