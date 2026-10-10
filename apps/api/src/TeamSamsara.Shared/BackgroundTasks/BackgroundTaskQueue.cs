// File : /team-samsara/apps/api/src/TeamSamsara.Shared/BackgroundTasks/BackgroundTaskQueue.cs
// Version : 1.0.0
// Latest commit: feat/background-task-queue
// Author : Gerrah
// Purpose : In-memory implementation of the background task queue, a bounded channel with a
// single reader (the worker). Bounded so a flood of requests cannot fill the app's memory:
// beyond the capacity, new work is refused instead of piling up.

using System.Threading.Channels;

namespace TeamSamsara.Shared.BackgroundTasks;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    #region Fields

    public const int DefaultCapacity = 1000;

    private readonly Channel<BackgroundWorkItem> _channel;

    #endregion

    #region Constructors

    public BackgroundTaskQueue(int capacity = DefaultCapacity)
    {
        _channel = Channel.CreateBounded<BackgroundWorkItem>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true
        });
    }

    #endregion

    #region Public Methods

    public bool TryEnqueue(BackgroundWorkItem workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);

        return _channel.Writer.TryWrite(workItem);
    }

    // Queued work, processed in FIFO order. For worker use only.
    public IAsyncEnumerable<BackgroundWorkItem> DequeueAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }

    #endregion
}
