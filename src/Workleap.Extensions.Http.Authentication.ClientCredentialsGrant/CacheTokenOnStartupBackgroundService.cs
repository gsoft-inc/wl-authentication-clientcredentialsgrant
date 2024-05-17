using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Workleap.Extensions.Http.Authentication.ClientCredentialsGrant;

internal sealed class CacheTokenOnStartupBackgroundService : BackgroundService
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IClientCredentialsTokenManagementService _tokenManagementService;
    private readonly IOptions<CacheTokenOnStartupBackgroundServiceOptions> _backgroundServiceOptions;
    private readonly SemaphoreSlim _allTokensCachedSignal;
    private readonly List<string> _clientNames;

    private CancellationTokenRegistration? _applicationStartedRegistration;
    private int _successfulCachedTokenCount;

    public CacheTokenOnStartupBackgroundService(
        IHostApplicationLifetime applicationLifetime,
        IClientCredentialsTokenManagementService tokenManagementService,
        IOptions<CacheTokenOnStartupBackgroundServiceOptions> backgroundServiceOptions)
    {
        this._applicationLifetime = applicationLifetime;
        this._tokenManagementService = tokenManagementService;
        this._backgroundServiceOptions = backgroundServiceOptions;
        this._allTokensCachedSignal = new SemaphoreSlim(initialCount: 0, maxCount: 1);
        this._clientNames = new List<string>();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this._clientNames.AddRange(this._backgroundServiceOptions.Value.ClientCredentialPoweredClientNames);

        this._applicationStartedRegistration = this._applicationLifetime.ApplicationStarted.Register(() =>
        {
            foreach (var clientName in this._clientNames)
            {
                // We don't await this task because we want to cache all tokens in parallel (fire and forget)
                _ = this.CacheTokenAsync(clientName, stoppingToken);
            }
        });

        return Task.CompletedTask;
    }

    private async Task CacheTokenAsync(string clientName, CancellationToken cancellationToken)
    {
        try
        {
            _ = await this._tokenManagementService.GetAccessTokenAsync(clientName, CachingBehavior.ForceRefresh, cancellationToken).ConfigureAwait(false);

            if (Interlocked.Increment(ref this._successfulCachedTokenCount) == this._clientNames.Count)
            {
                this._allTokensCachedSignal.Release();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected when the application is shutting down
        }
        catch
        {
            // We did our best to cache the token on startup. It will be retried when an HttpClient will attempt to make authenticated requests.
        }
    }

    // This is meant to be used for integration tests only
    internal async Task WaitForTokenCachingToCompleteAsync(CancellationToken cancellationToken)
    {
        await this._allTokensCachedSignal.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public override void Dispose()
    {
        this._allTokensCachedSignal.Dispose();
        this._applicationStartedRegistration?.Dispose();

        base.Dispose();
    }
}