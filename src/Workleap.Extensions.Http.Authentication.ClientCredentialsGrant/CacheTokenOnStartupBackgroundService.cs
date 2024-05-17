using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Workleap.Extensions.Http.Authentication.ClientCredentialsGrant;

internal sealed class CacheTokenOnStartupBackgroundService : BackgroundService
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IClientCredentialsTokenManagementService _tokenManagementService;
    private readonly IOptions<CacheTokenOnStartupBackgroundServiceOptions> _backgroundServiceOptions;

    private CancellationTokenRegistration? _applicationStartedRegistration;

    public CacheTokenOnStartupBackgroundService(
        IHostApplicationLifetime applicationLifetime,
        IClientCredentialsTokenManagementService tokenManagementService,
        IOptions<CacheTokenOnStartupBackgroundServiceOptions> backgroundServiceOptions)
    {
        this._applicationLifetime = applicationLifetime;
        this._tokenManagementService = tokenManagementService;
        this._backgroundServiceOptions = backgroundServiceOptions;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this._applicationStartedRegistration = this._applicationLifetime.ApplicationStarted.Register(() =>
        {
            foreach (var clientName in this._backgroundServiceOptions.Value.ClientCredentialPoweredClientNames)
            {
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

    public override void Dispose()
    {
        this._applicationStartedRegistration?.Dispose();

        base.Dispose();
    }
}