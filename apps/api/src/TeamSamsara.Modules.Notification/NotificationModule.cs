// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification/NotificationModule.cs
// Version : 1.3.0
// Latest commit: task/notifications-api
// Author : Gerrah
// Purpose : Registers the Notification module's services and maps the member inbox endpoints.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TeamSamsara.Modules.Notification.Endpoints;
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
        services.AddScoped<IInboxService, InboxService>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IMemberDataEraser, NotificationMemberDataEraser>());
    }

    // Maps the inbox endpoints under /notifications, for signed-in callers only; each endpoint then requires a verified member.
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(NotificationRoutes.Group)
            .RequireAuthorization();

        NotificationEndpoints.Map(group);
    }

    #endregion
}
