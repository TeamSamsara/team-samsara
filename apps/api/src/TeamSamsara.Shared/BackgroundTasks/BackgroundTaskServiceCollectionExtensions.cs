// File : /team-samsara/apps/api/src/TeamSamsara.Shared/BackgroundTasks/BackgroundTaskServiceCollectionExtensions.cs
// Version : 1.0.0
// Latest commit: feat/background-task-queue
// Author : Gerrah
// Purpose : Registers the background task queue and its worker. Called directly from Program.cs,
// the same way AddEmail and AddFirestore are - background work is cross-cutting infrastructure.

using Microsoft.Extensions.DependencyInjection;

namespace TeamSamsara.Shared.BackgroundTasks;

public static class BackgroundTaskServiceCollectionExtensions
{
    #region Public Methods

    public static IServiceCollection AddBackgroundTasks(this IServiceCollection services)
    {
        // One queue object, seen by callers as the interface and by the worker as the class
        services.AddSingleton(_ => new BackgroundTaskQueue());
        services.AddSingleton<IBackgroundTaskQueue>(provider => provider.GetRequiredService<BackgroundTaskQueue>());
        services.AddHostedService<BackgroundTaskWorker>();

        return services;
    }

    #endregion
}
