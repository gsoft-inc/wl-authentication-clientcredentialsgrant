using System.Text.Json.Serialization;

namespace GSoft.Extensions.Http.Authentication.ClientCredentialsGrant;

internal sealed class OpenIdConfiguration
{
    [JsonPropertyName("token_endpoint")]
    public string TokenEndpoint { get; set; } = string.Empty;
}