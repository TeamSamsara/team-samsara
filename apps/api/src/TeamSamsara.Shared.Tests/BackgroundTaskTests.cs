// File : /team-samsara/apps/api/src/TeamSamsara.Shared.Tests/BackgroundTaskTests.cs
// Version : 1.0.1
// Latest commit: feat/background-task-queue
// Author : Gerrah
// Purpose : Proves the background task queue: queued work runs in order, one item at a time,
// each in a scope of its own that is disposed afterwards, a failing item does not stop the
// worker, a full queue refuses work, and shutting down cancels a running item without hanging.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shouldly;
using TeamSamsara.Shared.BackgroundTasks;

namespace TeamSamsara.Shared.Tests;

public class BackgroundTaskTests
{
    #region Fields

    private static readonly TimeSpan _waitLimit = TimeSpan.FromSeconds(5);

    #endregion

    #region Public Methods

    [Fact]
    public async Task AQueuedItem_Runs()
    {
        using var host = BuildHost();
        await host.StartAsync();
        var queue = host.Services.GetRequiredService<IBackgroundTaskQueue>();
        var ran = new TaskCompletionSource();

        var accepted = queue.TryEnqueue((_, _) =>
        {
            ran.SetResult();
            return Task.CompletedTask;
        });

        accepted.ShouldBeTrue();
        await ran.Task.WaitAsync(_waitLimit);
    }

    [Fact]
    public async Task QueuedItems_RunInTheOrderTheyWereAdded()
    {
        using var host = BuildHost();
        await host.StartAsync();
        var queue = host.Services.GetRequiredService<IBackgroundTaskQueue>();
        var order = new List<int>();
        var done = new TaskCompletionSource();

        for (var number = 1; number <= 3; number++)
        {
            var current = number;

            queue.TryEnqueue((_, _) =>
            {
                order.Add(current);

                if (current == 3)
                {
                    done.SetResult();
                }

                return Task.CompletedTask;
            });
        }

        await done.Task.WaitAsync(_waitLimit);
        order.ShouldBe(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task EachItem_GetsAScopeOfItsOwn_DisposedWhenTheItemIsDone()
    {
        using var host = BuildHost(services => services.AddScoped<ScopedMarker>());
        await host.StartAsync();
        var queue = host.Services.GetRequiredService<IBackgroundTaskQueue>();
        ScopedMarker? first = null;
        ScopedMarker? second = null;
        var done = new TaskCompletionSource();

        queue.TryEnqueue((services, _) =>
        {
            first = services.GetRequiredService<ScopedMarker>();
            return Task.CompletedTask;
        });
        queue.TryEnqueue((services, _) =>
        {
            second = services.GetRequiredService<ScopedMarker>();
            done.SetResult();
            return Task.CompletedTask;
        });

        await done.Task.WaitAsync(_waitLimit);

        first.ShouldNotBeNull();
        second.ShouldNotBeNull();
        second.ShouldNotBeSameAs(first);
        first.Disposed.ShouldBeTrue();
    }

    [Fact]
    public async Task AFailingItem_DoesNotStopTheWorker()
    {
        using var host = BuildHost();
        await host.StartAsync();
        var queue = host.Services.GetRequiredService<IBackgroundTaskQueue>();
        var ran = new TaskCompletionSource();

        queue.TryEnqueue((_, _) => throw new InvalidOperationException("Simulated failure."));
        queue.TryEnqueue((_, _) =>
        {
            ran.SetResult();
            return Task.CompletedTask;
        });

        await ran.Task.WaitAsync(_waitLimit);
    }

    [Fact]
    public void AFullQueue_RefusesNewWork()
    {
        var queue = new BackgroundTaskQueue(capacity: 2);

        queue.TryEnqueue(DoNothing).ShouldBeTrue();
        queue.TryEnqueue(DoNothing).ShouldBeTrue();
        queue.TryEnqueue(DoNothing).ShouldBeFalse();
    }

    [Fact]
    public async Task ShuttingDown_CancelsARunningItem_AndDoesNotHang()
    {
        using var host = BuildHost();
        await host.StartAsync();
        var queue = host.Services.GetRequiredService<IBackgroundTaskQueue>();
        var started = new TaskCompletionSource();
        var cancelled = new TaskCompletionSource();

        queue.TryEnqueue(async (_, cancellationToken) =>
        {
            started.SetResult();

            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                cancelled.SetResult();
                throw;
            }
        });

        await started.Task.WaitAsync(_waitLimit);
        await host.StopAsync().WaitAsync(_waitLimit);
        await cancelled.Task.WaitAsync(_waitLimit);
    }

    #endregion

    #region Private Methods

    private static IHost BuildHost(Action<IServiceCollection>? configure = null)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddBackgroundTasks();
        configure?.Invoke(builder.Services);

        return builder.Build();
    }

    private static Task DoNothing(IServiceProvider services, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    #endregion

    #region Nested Types

    private sealed class ScopedMarker : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose()
        {
            Disposed = true;
        }
    }

    #endregion
}
