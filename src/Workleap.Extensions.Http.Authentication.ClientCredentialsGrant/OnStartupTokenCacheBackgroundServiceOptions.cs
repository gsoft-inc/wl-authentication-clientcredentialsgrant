namespace Workleap.Extensions.Http.Authentication.ClientCredentialsGrant;

internal sealed class OnStartupTokenCacheBackgroundServiceOptions
{
    public HashSet<string> ClientCredentialsPoweredClientNames { get; } = new HashSet<string>(StringComparer.Ordinal);
}