// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification/NotificationModule.cs
// Version : 1.2.0
// Latest commit: feat/notification-erasure
// Author : Gerrah
// Purpose : Registers the Notification module's services; it has no HTTP endpoints yet.

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TeamSamsara.Modules.Notification.Handlers;
using TeamSamsara.Modules.Notification.Models;
using TeamSamsara.Modules.Notification.Repositories;
using TeamSamsara.Shared.Configuration;
using TeamSamsara.Shared.Context;
using TeamSamsara.Shared.Members;
using TeamSamsara.Shared.Modules;
using TeamSamsara.Shared.Notification;

namespace TeamSamsara.Modules.Notification;

public class NotificationModule : IModule
{
    #region Public Methods

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatedOptions<NotificationSettings>(configuration, NotificationSettings.SectionName);

        services.TryAddSingleton<IClock, SystemClock>();
        services.TryAddSingleton<IIdGenerator, GuidV7IdGenerator>();

        services.AddScoped<IInboxNotificationStore, FirestoreInboxNotificationStore>();
        services.AddScoped<INotificationSender, NotificationSender>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IMemberDataEraser, NotificationMemberDataEraser>());
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Nothing to map yet - the inbox endpoints come later.
    }

    #endregion
}
