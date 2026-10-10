// File : /team-samsara/apps/api/src/TeamSamsara.Shared/BackgroundTasks/IBackgroundTaskQueue.cs
// Version : 1.0.0
// Latest commit: feat/background-task-queue
// Author : Gerrah
// Purpose : Lets a request hand slow work to a background worker and
// answer straight away, so the time the request takes does not depend on that work.

namespace TeamSamsara.Shared.BackgroundTasks;

public interface IBackgroundTaskQueue
{
    // Queues work for sequential execution after the current request.
    // Returns false if the queue is full. Queued work is lost on restart.
    public bool TryEnqueue(BackgroundWorkItem workItem);
}
