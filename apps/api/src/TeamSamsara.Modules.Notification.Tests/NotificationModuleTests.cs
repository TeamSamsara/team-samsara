// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification.Tests/NotificationModuleTests.cs
// Version : 1.0.0
// Latest commit: feat/notification-erasure
// Author : Gerrah
// Purpose : Proves the module's registrations resolve and that its eraser is offered to the account purge exactly once.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TeamSamsara.Modules.Notification.Handlers;
using TeamSamsara.Modules.Notification.Repositories;
using TeamSamsara.Modules.Notification.Tests.Fakes;
using TeamSamsara.Shared.Members;
using TeamSamsara.Shared.Notification;

namespace TeamSamsara.Modules.Notification.Tests;

public class NotificationModuleTests
{
    #region Public Methods

    [Fact]
    public void TheModule_OffersItsEraserToTheAccountPurge()
    {
        using var provider = BuildProvider(registrations: 1);
        using var scope = provider.CreateScope();

        var erasers = scope.ServiceProvider.GetServices<IMemberDataEraser>();

        erasers.ShouldHaveSingleItem().ShouldBeOfType<NotificationMemberDataEraser>();
    }

    [Fact]
    public void RegisteringTheModuleTwice_StillOffersTheEraserOnce()
    {
        using var provider = BuildProvider(registrations: 2);
        using var scope = provider.CreateScope();

        var erasers = scope.ServiceProvider.GetServices<IMemberDataEraser>();

        erasers.ShouldHaveSingleItem();
    }

    [Fact]
    public void TheModule_ResolvesItsSender()
    {
        using var provider = BuildProvider(registrations: 1);
        using var scope = provider.CreateScope();

        var sender = scope.ServiceProvider.GetRequiredService<INotificationSender>();

        sender.ShouldBeOfType<NotificationSender>();
    }

    #endregion

    #region Private Methods

    // Registers the module the given number of times over an in-memory store, since there is no Firestore here
    private static ServiceProvider BuildProvider(int registrations)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        var module = new NotificationModule();

        services.AddLogging();

        for (var index = 0; index < registrations; index++)
        {
            module.RegisterServices(services, configuration);
        }

        services.AddScoped<IInboxNotificationStore, InMemoryInboxNotificationStore>();

        return services.BuildServiceProvider();
    }

    #endregion
}
