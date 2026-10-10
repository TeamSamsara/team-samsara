// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/Fakes/RecordingBackgroundTaskQueue.cs
// Version : 1.0.0
// Latest commit: feat/password-reset-service
// Author : Gerrah
// Purpose : Background queue that holds work until a test runs it, and can simulate a full queue.

using Microsoft.Extensions.DependencyInjection;
using TeamSamsara.Shared.BackgroundTasks;

namespace TeamSamsara.Modules.Identity.Tests.Fakes;

public class RecordingBackgroundTaskQueue : IBackgroundTaskQueue
{
    #region Fields

    private readonly List<BackgroundWorkItem> _items = new();

    #endregion

    #region Properties

    // When false, the queue refuses new work, like a full queue.
    public bool Accepting { get; set; } = true;

    // Number of items waiting to run.
    public int Count => _items.Count;

    #endregion

    #region Public Methods

    public bool TryEnqueue(BackgroundWorkItem workItem)
    {
        if (!Accepting)
        {
            return false;
        }

        _items.Add(workItem);

        return true;
    }

    // Test helper: runs the waiting items in order, each in its own scope.
    public async Task RunAllAsync(IServiceProvider services)
    {
        var pending = _items.ToList();
        _items.Clear();

        foreach (var item in pending)
        {
            await using var scope = services.CreateAsyncScope();
            await item(scope.ServiceProvider, CancellationToken.None);
        }
    }

    #endregion
}
