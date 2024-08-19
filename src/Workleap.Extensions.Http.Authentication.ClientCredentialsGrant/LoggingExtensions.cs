using Microsoft.Extensions.Logging;

namespace Workleap.Extensions.Http.Authentication.ClientCredentialsGrant;

internal static partial class LoggingExtensions
{
    // ClientCredentialsTokenCache
    [LoggerMessage(1, LogLevel.Debug, "Caching token for client ID {ClientId} with cache key {CacheKey} for {CacheDuration}")]
    public static partial void CachingToken(this ILogger logger, string clientId, string cacheKey, TimeSpan cacheDuration);

    [LoggerMessage(2, LogLevel.Debug, "Successfully read token for client ID {ClientId} from L1 cache with cache key {CacheKey}, it will expire in {TimeToLive}")]
    public static partial void SuccessfullyReadTokenFromL1Cache(this ILogger logger, string clientId, string cacheKey, TimeSpan timeToLive);

    [LoggerMessage(3, LogLevel.Debug, "Successfully read token for client ID {ClientId} from L2 cache with cache key {CacheKey}, it will expire in {TimeToLive}")]
    public static partial void SuccessfullyReadTokenFromL2Cache(this ILogger logger, string clientId, string cacheKey, TimeSpan timeToLive);

    [LoggerMessage(4, LogLevel.Warning, "Client credentials token cache should not be using an in-memory distributed cache implementation")]
    public static partial void DistributedCacheIsUsingInMemoryImplementation(this ILogger logger);

    // ClientCredentialsTokenEndpointService
    [LoggerMessage(5, LogLevel.Debug, "Requesting new token for client ID {ClientId}")]
    public static partial void RequestingNewTokenForClient(this ILogger logger, string clientId);

    // ClientCredentialsTokenManagementService
    [LoggerMessage(6, LogLevel.Debug, "Getting a token for client ID {ClientId} with caching behavior {CachingBehavior}")]
    public static partial void GettingTokenForClientWithCachingBehavior(this ILogger logger, string clientId, CachingBehavior cachingBehavior);

    [LoggerMessage(7, LogLevel.Debug, "Scheduling background token refresh for client ID {ClientId} in {Delay}")]
    public static partial void SchedulingBackgroundTokenRefresh(this ILogger logger, string clientId, TimeSpan delay);

    [LoggerMessage(8, LogLevel.Debug, "Executing background token refresh for client ID {ClientId}")]
    public static partial void ExecutingBackgroundTokenRefresh(this ILogger logger, string clientId);
}