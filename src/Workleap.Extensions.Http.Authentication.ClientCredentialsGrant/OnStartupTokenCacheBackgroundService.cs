using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Workleap.Extensions.Http.Authentication.ClientCredentialsGrant;

internal sealed class OnStartupTokenCacheBackgroundService : BackgroundService
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IClientCredentialsTokenManagementService _tokenManagementService;
    private readonly IOptions<OnStartupTokenCacheBackgroundServiceOptions> _backgroundServiceOptions;
    private readonly IOptionsMonitor<ClientCredentialsOptions> _clientCredentialsOptionsMonitor;
    private readonly TaskCompletionSource<bool> _allTokensCachedSignal;
    private readonly List<string> _clientNames;

    private CancellationTokenRegistration? _applicationStartedRegistration;
    private int _successfullyCachedTokenCount;

    public OnStartupTokenCacheBackgroundService(
        IHostApplicationLifetime applicationLifetime,
        IClientCredentialsTokenManagementService tokenManagementService,
        IOptions<OnStartupTokenCacheBackgroundServiceOptions> backgroundServiceOptions,
        IOptionsMonitor<ClientCredentialsOptions> clientCredentialsOptionsMonitor)
    {
        this._applicationLifetime = applicationLifetime;
        this._tokenManagementService = tokenManagementService;
        this._backgroundServiceOptions = backgroundServiceOptions;
        this._clientCredentialsOptionsMonitor = clientCredentialsOptionsMonitor;
        this._allTokensCachedSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        this._clientNames = new List<string>();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var clientName in this._backgroundServiceOptions.Value.ClientCredentialsPoweredClientNames)
        {
            var options = this._clientCredentialsOptionsMonitor.Get(clientName);
            if (options.EnablePeriodicTokenBackgroundRefresh)
            {
                this._clientNames.Add(clientName);
            }
        }

        this._applicationStartedRegistration = this._applicationLifetime.ApplicationStarted.Register(() =>
        {
            foreach (var clientName in this._clientNames)
            {
                // We don't await this task because we want to cache all tokens in parallel
                this.CacheTokenAsync(clientName, stoppingToken).Forget();
            }
        });

        return Task.CompletedTask;
    }

    private async Task CacheTokenAsync(string clientName, CancellationToken cancellationToken)
    {
        try
        {
            var options = this._clientCredentialsOptionsMonitor.Get(clientName);
            if (!options.EnablePeriodicTokenBackgroundRefresh)
            {
                return;
            }

            _ = await this._tokenManagementService.GetAccessTokenAsync(clientName, CachingBehavior.ForceRefresh, cancellationToken).ConfigureAwait(false);

            if (Interlocked.Increment(ref this._successfullyCachedTokenCount) == this._clientNames.Count)
            {
                this._allTokensCachedSignal.SetResult(true);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected when the application is shutting down
            this._allTokensCachedSignal.TrySetCanceled(cancellationToken);
        }
        catch (Exception ex)
        {
            // We did our best to cache the token on startup. It will be retried when an HttpClient will attempt to make authenticated requests.
            this._allTokensCachedSignal.TrySetException(ex);
        }
    }

    // This is meant to be used for integration tests only
    internal async Task WaitForTokenCachingToCompleteAsync(CancellationToken cancellationToken)
    {
        if (this._clientNames.Count > 0)
        {
            var timeoutTask = Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            var completedTask = await Task.WhenAny(this._allTokensCachedSignal.Task, timeoutTask).ConfigureAwait(false);
            await completedTask.ConfigureAwait(false);
        }
    }

    public override void Dispose()
    {
        this._applicationStartedRegistration?.Dispose();

        base.Dispose();
    }
}