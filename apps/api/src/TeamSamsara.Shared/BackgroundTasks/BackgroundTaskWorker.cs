// File : /team-samsara/apps/api/src/TeamSamsara.Shared/BackgroundTasks/BackgroundTaskWorker.cs
// Version : 1.0.1
// Latest commit: feat/background-task-queue
// Author : Gerrah
// Purpose: Executes queued work sequentially in isolated service scopes.
// Logs failures without interrupting the worker.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TeamSamsara.Shared.BackgroundTasks;

public class BackgroundTaskWorker : BackgroundService
{
    #region Fields
    private readonly BackgroundTaskQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundTaskWorker> _logger;
    #endregion

    #region Constructors

    public BackgroundTaskWorker(
        BackgroundTaskQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<BackgroundTaskWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }
    #endregion

    #region Protected Methods

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var workItem in _queue.DequeueAllAsync(stoppingToken))
            {
                await RunAsync(workItem, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected when stoppingToken is cancelled.
            // Work still waiting in the queue is dropped.
        }
    }
    #endregion

    #region Private Methods

    private async Task RunAsync(BackgroundWorkItem workItem, CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            await workItem(scope.ServiceProvider, stoppingToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Background work item failed.");
        }
    }
    #endregion
}
