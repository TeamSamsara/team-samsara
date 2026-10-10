// File : /team-samsara/apps/api/src/TeamSamsara.Shared/BackgroundTasks/BackgroundWorkItem.cs
// Version : 1.0.0
// Latest commit: feat/background-task-queue
// Author : Gerrah
// Purpose : A piece of work handed to the background task queue.

namespace TeamSamsara.Shared.BackgroundTasks;

public delegate Task BackgroundWorkItem(IServiceProvider serviceProvider, CancellationToken cancellationToken);
