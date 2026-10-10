// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/AccountPurgeWorkerTests.cs
// Version : 1.0.0
// Latest commit: feat/account-purge-job
// Author : Gerrah
// Purpose : Proves the purge worker runs at startup, uses a fresh scope per run, reports failures, and never lets one failed run stop the job.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shouldly;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Workers;

namespace TeamSamsara.Modules.Identity.Tests;

public class AccountPurgeWorkerTests
{
    #region Fields

    private readonly FakePurgeService _purge = new();
    private readonly RecordingLogger<AccountPurgeWorker> _logger = new();

    #endregion

    #region Public Methods

    [Fact]
    public async Task RunOnce_RunsThePurge()
    {
        var worker = CreateWorker();

        await worker.RunOnceAsync();

        _purge.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task RunOnce_UsesAFreshScopeForEveryRun()
    {
        var scopes = 0;
        var services = new ServiceCollection();
        services.AddScoped<IAccountPurgeService>(_ =>
        {
            scopes++;
            return _purge;
        });
        var worker = CreateWorker(services);

        await worker.RunOnceAsync();
        await worker.RunOnceAsync();

        scopes.ShouldBe(2);
        _purge.Calls.ShouldBe(2);
    }

    [Fact]
    public async Task RunOnce_AFailedRun_IsLoggedAndDoesNotThrow()
    {
        _purge.Behavior = _ => throw new InvalidOperationException("database unavailable");
        var worker = CreateWorker();

        await Should.NotThrowAsync(() => worker.RunOnceAsync());

        _logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Error);
    }

    [Fact]
    public async Task RunOnce_AfterAFailedRun_TheNextRunStillPurges()
    {
        _purge.Behavior = _ => throw new InvalidOperationException("database unavailable");
        var worker = CreateWorker();
        await worker.RunOnceAsync();

        _purge.Behavior = _ => Task.FromResult(new AccountPurgeResult(2, 0, 0));
        await worker.RunOnceAsync();

        _purge.Calls.ShouldBe(2);
        _logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Information);
    }

    [Fact]
    public async Task RunOnce_FailedAccounts_AreLoggedAsAWarning()
    {
        _purge.Behavior = _ => Task.FromResult(new AccountPurgeResult(3, 0, 1));
        var worker = CreateWorker();

        await worker.RunOnceAsync();

        _logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning);
        _logger.Entries.ShouldNotContain(entry => entry.Level == LogLevel.Error);
    }

    [Fact]
    public async Task RunOnce_NothingToPurge_LogsNothing()
    {
        var worker = CreateWorker();

        await worker.RunOnceAsync();

        _logger.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunOnce_WhenTheHostIsStopping_ReturnsWithoutAnError()
    {
        using var cancellation = new CancellationTokenSource();
        _purge.Behavior = token =>
        {
            cancellation.Cancel();
            token.ThrowIfCancellationRequested();
            return Task.FromResult(new AccountPurgeResult(0, 0, 0));
        };
        var worker = CreateWorker();

        await Should.NotThrowAsync(() => worker.RunOnceAsync(cancellation.Token));

        _logger.Entries.ShouldNotContain(entry => entry.Level == LogLevel.Error);
    }

    [Fact]
    public async Task Start_PurgesRightAwayAndStopsCleanly()
    {
        var worker = CreateWorker();

        await worker.StartAsync(CancellationToken.None);
        await _purge.FirstCall.WaitAsync(TimeSpan.FromSeconds(5));
        await worker.StopAsync(CancellationToken.None);

        _purge.Calls.ShouldBe(1);
        worker.ExecuteTask!.IsCompletedSuccessfully.ShouldBeTrue();
    }

    [Fact]
    public async Task Start_AFailingFirstRun_DoesNotCrashTheWorker()
    {
        _purge.Behavior = _ => throw new InvalidOperationException("database unavailable");
        var worker = CreateWorker();

        await worker.StartAsync(CancellationToken.None);
        await _purge.FirstCall.WaitAsync(TimeSpan.FromSeconds(5));
        worker.ExecuteTask!.IsFaulted.ShouldBeFalse();
        await worker.StopAsync(CancellationToken.None);

        worker.ExecuteTask.IsCompletedSuccessfully.ShouldBeTrue();
    }

    #endregion

    #region Private Methods

    // Builds a worker over a real scope factory, registering the fake purge unless the caller supplied one
    private AccountPurgeWorker CreateWorker(ServiceCollection? services = null)
    {
        services ??= new ServiceCollection();

        if (!services.Any(descriptor => descriptor.ServiceType == typeof(IAccountPurgeService)))
        {
            services.AddSingleton<IAccountPurgeService>(_purge);
        }

        var provider = services.BuildServiceProvider();
        var settings = Options.Create(new AccountPurgeSettings { IntervalHours = 1 });

        return new AccountPurgeWorker(provider.GetRequiredService<IServiceScopeFactory>(), settings, _logger);
    }

    #endregion

    #region Nested Types

    // Purge service whose result is set per test and which counts its calls
    private sealed class FakePurgeService : IAccountPurgeService
    {
        private readonly TaskCompletionSource _firstCall = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Calls { get; private set; }

        public Task FirstCall => _firstCall.Task;

        public Func<CancellationToken, Task<AccountPurgeResult>> Behavior { get; set; } =
            _ => Task.FromResult(new AccountPurgeResult(0, 0, 0));

        public async Task<AccountPurgeResult> PurgeExpiredAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            _firstCall.TrySetResult();
            return await Behavior(cancellationToken);
        }
    }

    // Logger that keeps what was written so tests can check levels
    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }

    #endregion
}
