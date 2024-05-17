namespace Workleap.Extensions.Http.Authentication.ClientCredentialsGrant;

internal sealed class CacheTokenOnStartupBackgroundServiceOptions
{
    public HashSet<string> ClientCredentialPoweredClientNames { get; } = new HashSet<string>(StringComparer.Ordinal);
}