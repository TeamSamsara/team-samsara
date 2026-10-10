// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Workers/AccountPurgeWorker.cs
// Version : 1.0.0
// Latest commit: feat/account-purge-job
// Author : Gerrah
// Purpose : Runs the account purge at startup and then every configured interval, surviving any failure so the next run always happens.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Workers;

public class AccountPurgeWorker : BackgroundService
{
    #region Fields

    private readonly IServiceScopeFactory _scopes;
    private readonly AccountPurgeSettings _settings;
    private readonly ILogger<AccountPurgeWorker> _logger;

    #endregion

    #region Constructors

    public AccountPurgeWorker(
        IServiceScopeFactory scopes,
        IOptions<AccountPurgeSettings> settings,
        ILogger<AccountPurgeWorker> logger)
    {
        _scopes = scopes;
        _settings = settings.Value;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    // Runs one purge in its own scope; any failure is logged and never thrown, only a shutdown cancels it
    public async Task RunOnceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var purge = scope.ServiceProvider.GetRequiredService<IAccountPurgeService>();
            var result = await purge.PurgeExpiredAsync(cancellationToken);
            LogResult(result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutting down
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Account purge run failed; the next run will retry");
        }
    }

    #endregion

    #region Protected Methods

    // Purges once at startup, then once per interval until the host stops
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        using var timer = new PeriodicTimer(TimeSpan.FromHours(_settings.IntervalHours));

        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (await WaitForNextTickAsync(timer, stoppingToken));
    }

    #endregion

    #region Private Methods

    // Waits for the next interval and answers false when the host is stopping
    private static async Task<bool> WaitForNextTickAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    // Logs the run summary, as a warning when some accounts could not be removed
    private void LogResult(AccountPurgeResult result)
    {
        if (result.Failed > 0)
        {
            _logger.LogWarning(
                "Account purge finished with failures: {Purged} purged, {Skipped} skipped, {Failed} failed",
                result.Purged, result.Skipped, result.Failed);
            return;
        }

        if (result.Purged > 0 || result.Skipped > 0)
        {
            _logger.LogInformation(
                "Account purge finished: {Purged} purged, {Skipped} skipped",
                result.Purged, result.Skipped);
        }
    }

    #endregion
}
