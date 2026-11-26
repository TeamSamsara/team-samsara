// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification.Tests/NotificationEndpointsTests.cs
// Version : 1.1.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Proves the inbox endpoints are for verified members only, show a member only their own notifications, page with a cursor, answer 400 for a malformed cursor and 404 for an unknown or foreign id, and let a member delete one notification (409 for an unanswered sign-in notice) or clear all the read ones.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TeamSamsara.Modules.Notification.Endpoints;
using TeamSamsara.Modules.Notification.Handlers;
using TeamSamsara.Modules.Notification.Models;
using TeamSamsara.Modules.Notification.Tests.Fakes;
using TeamSamsara.Shared.Context;
using TeamSamsara.Shared.Notification;

namespace TeamSamsara.Modules.Notification.Tests;

public class NotificationEndpointsTests
{
    #region Fields

    private const string Member = "uid-member-1";
    private const string OtherMember = "uid-member-2";
    private const string Key = "order.shipped";
    private const string KnownId = "00000000000000000000000000000001";
    private const string UnknownId = "00000000000000000000000000000fff";
    private const int RetentionDays = 90;

    private static readonly (string Method, string Path)[] _routes =
    {
        ("GET", NotificationRoutes.Root),
        ("GET", NotificationRoutes.UnreadCount),
        ("POST", NotificationRoutes.ReadAll),
        ("POST", NotificationRoutes.Read.Replace("{id}", KnownId)),
        ("DELETE", NotificationRoutes.Item.Replace("{id}", KnownId)),
        ("DELETE", NotificationRoutes.ClearRead)
    };

    private readonly InMemoryInboxNotificationStore _store = new();
    private readonly FakeClock _clock = new();
    private int _lastId;

    #endregion

    #region Public Methods

    public static TheoryData<string, string> AnonymousCases()
    {
        var data = new TheoryData<string, string>();

        foreach (var (method, path) in _routes)
        {
            data.Add(method, path);
        }

        return data;
    }

    public static TheoryData<string, string, AuthState> UnverifiedCases()
    {
        var data = new TheoryData<string, string, AuthState>();

        foreach (var (method, path) in _routes)
        {
            data.Add(method, path, AuthState.RegistrationIncomplete);
            data.Add(method, path, AuthState.VerificationRequired);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AnonymousCases))]
    public async Task AnAnonymousCaller_GetsUnauthorized(string method, string path)
    {
        await using var app = await StartAsync(AuthState.Anonymous);

        var response = await SendAsync(app, method, path);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [MemberData(nameof(UnverifiedCases))]
    public async Task AnUnverifiedCaller_GetsForbidden_AndNothingIsChanged(string method, string path, AuthState authState)
    {
        var stored = await SeedAsync(Member, minutesAgo: 1, id: KnownId);
        await using var app = await StartAsync(authState);

        var response = await SendAsync(app, method, path);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        ReadAtOf(stored.Id).ShouldBeNull();
    }

    [Fact]
    public async Task List_ForAMember_ReturnsTheirNotifications_NewestFirst_WithoutACategory()
    {
        var older = await SeedAsync(Member, minutesAgo: 10);
        var newer = await SeedAsync(Member, minutesAgo: 5, readAt: _clock.UtcNow);
        await SeedAsync(OtherMember, minutesAgo: 1);
        await using var app = await StartAsync(AuthState.Member);

        var response = await SendAsync(app, "GET", NotificationRoutes.Root);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = json.GetProperty("items");

        items.GetArrayLength().ShouldBe(2);
        items[0].GetProperty("id").GetString().ShouldBe(newer.Id);
        items[0].GetProperty("key").GetString().ShouldBe(Key);
        items[0].GetProperty("data").GetProperty("orderNumber").GetString().ShouldBe("1001");
        items[0].GetProperty("readAt").ValueKind.ShouldBe(JsonValueKind.String);
        items[0].TryGetProperty("category", out _).ShouldBeFalse();
        items[1].GetProperty("id").GetString().ShouldBe(older.Id);
        items[1].GetProperty("readAt").ValueKind.ShouldBe(JsonValueKind.Null);
        json.GetProperty("nextCursor").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task List_WithALimit_OffersACursorThatLeadsToTheRest()
    {
        var older = await SeedAsync(Member, minutesAgo: 10);
        var newer = await SeedAsync(Member, minutesAgo: 5);
        await using var app = await StartAsync(AuthState.Member);

        var first = await (await SendAsync(app, "GET", NotificationRoutes.Root + "?limit=1"))
            .Content.ReadFromJsonAsync<JsonElement>();

        first.GetProperty("items").GetArrayLength().ShouldBe(1);
        first.GetProperty("items")[0].GetProperty("id").GetString().ShouldBe(newer.Id);

        var cursor = first.GetProperty("nextCursor").GetString();
        cursor.ShouldNotBeNull();

        var second = await (await SendAsync(app, "GET", NotificationRoutes.Root + "?limit=1&cursor=" + Uri.EscapeDataString(cursor)))
            .Content.ReadFromJsonAsync<JsonElement>();

        second.GetProperty("items").GetArrayLength().ShouldBe(1);
        second.GetProperty("items")[0].GetProperty("id").GetString().ShouldBe(older.Id);
        second.GetProperty("nextCursor").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task List_WithAMalformedCursor_GetsBadRequest()
    {
        await SeedAsync(Member, minutesAgo: 1);
        await using var app = await StartAsync(AuthState.Member);

        var response = await SendAsync(app, "GET", NotificationRoutes.Root + "?cursor=garbage");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UnreadCount_ForAMember_CountsOnlyTheirUnreadNotifications()
    {
        await SeedAsync(Member, minutesAgo: 1);
        await SeedAsync(Member, minutesAgo: 2);
        await SeedAsync(Member, minutesAgo: 3, readAt: _clock.UtcNow);
        await SeedAsync(OtherMember, minutesAgo: 1);
        await using var app = await StartAsync(AuthState.Member);

        var response = await SendAsync(app, "GET", NotificationRoutes.UnreadCount);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("unreadCount").GetInt32().ShouldBe(2);
    }

    [Fact]
    public async Task MarkRead_ForAMember_GetsNoContent_AndMarksThatNotification()
    {
        var stored = await SeedAsync(Member, minutesAgo: 1);
        var untouched = await SeedAsync(Member, minutesAgo: 2);
        await using var app = await StartAsync(AuthState.Member);

        var response = await SendAsync(app, "POST", ReadPath(stored.Id));

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        ReadAtOf(stored.Id).ShouldBe(_clock.UtcNow);
        ReadAtOf(untouched.Id).ShouldBeNull();
    }

    [Fact]
    public async Task MarkRead_Twice_GetsNoContentBothTimes()
    {
        var stored = await SeedAsync(Member, minutesAgo: 1);
        await using var app = await StartAsync(AuthState.Member);

        var first = await SendAsync(app, "POST", ReadPath(stored.Id));
        var second = await SendAsync(app, "POST", ReadPath(stored.Id));

        first.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        second.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task MarkRead_OfAnUnknownId_GetsNotFound()
    {
        await using var app = await StartAsync(AuthState.Member);

        var response = await SendAsync(app, "POST", ReadPath(UnknownId));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MarkRead_OfAnotherMembersNotification_GetsNotFound_AndChangesNothing()
    {
        var foreign = await SeedAsync(OtherMember, minutesAgo: 1);
        await using var app = await StartAsync(AuthState.Member);

        var response = await SendAsync(app, "POST", ReadPath(foreign.Id));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ReadAtOf(foreign.Id).ShouldBeNull();
    }

    [Fact]
    public async Task MarkRead_WithAMalformedId_GetsNotFound()
    {
        await using var app = await StartAsync(AuthState.Member);

        var response = await SendAsync(app, "POST", ReadPath("not-an-id"));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        _store.MarkReadCalls.ShouldBe(0);
    }

    [Fact]
    public async Task ReadAll_ForAMember_GetsNoContent_AndMarksOnlyTheirNotifications()
    {
        var mine = await SeedAsync(Member, minutesAgo: 1);
        var alsoMine = await SeedAsync(Member, minutesAgo: 2);
        var foreign = await SeedAsync(OtherMember, minutesAgo: 1);
        await using var app = await StartAsync(AuthState.Member);

        var response = await SendAsync(app, "POST", NotificationRoutes.ReadAll);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        ReadAtOf(mine.Id).ShouldBe(_clock.UtcNow);
        ReadAtOf(alsoMine.Id).ShouldBe(_clock.UtcNow);
        ReadAtOf(foreign.Id).ShouldBeNull();
    }

    [Fact]
    public async Task Delete_ForAMember_GetsNoContent_AndRemovesOnlyThatNotification()
    {
        var stored = await SeedAsync(Member, minutesAgo: 1);
        var untouched = await SeedAsync(Member, minutesAgo: 2);
        await using var app = await StartAsync(AuthState.Member);

        var response = await SendAsync(app, "DELETE", ItemPath(stored.Id));

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        _store.Documents.Select(document => document.Id).ShouldBe(new[] { untouched.Id });
    }

    [Fact]
    public async Task Delete_OfAnUnknownId_GetsNotFound()
    {
        await using var app = await StartAsync(AuthState.Member);

        var response = await SendAsync(app, "DELETE", ItemPath(UnknownId));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_OfAnotherMembersNotification_GetsNotFound_AndChangesNothing()
    {
        var foreign = await SeedAsync(OtherMember, minutesAgo: 1);
        await using var app = await StartAsync(AuthState.Member);

        var response = await SendAsync(app, "DELETE", ItemPath(foreign.Id));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        _store.Documents.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Delete_WithAMalformedId_GetsNotFound_WithoutTouchingStorage()
    {
        await using var app = await StartAsync(AuthState.Member);

        var response = await SendAsync(app, "DELETE", ItemPath("not-an-id"));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        _store.DeleteCalls.ShouldBe(0);
    }

    [Fact]
    public async Task Delete_OfAnUnansweredSignIn_GetsConflict_AndItStays()
    {
        var signIn = await SeedAsync(Member, minutesAgo: 1, key: NotificationKeys.NewLogin);
        await using var app = await StartAsync(AuthState.Member);

        var response = await SendAsync(app, "DELETE", ItemPath(signIn.Id));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        _store.Documents.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Delete_OfAnAnsweredSignIn_GetsNoContent()
    {
        var signIn = await SeedAsync(Member, minutesAgo: 1, readAt: _clock.UtcNow, key: NotificationKeys.NewLogin);
        await using var app = await StartAsync(AuthState.Member);

        var response = await SendAsync(app, "DELETE", ItemPath(signIn.Id));

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        _store.Documents.ShouldBeEmpty();
    }

    [Fact]
    public async Task ClearRead_ForAMember_GetsNoContent_AndLeavesTheUnreadOnes()
    {
        await SeedAsync(Member, minutesAgo: 1, readAt: _clock.UtcNow);
        var unread = await SeedAsync(Member, minutesAgo: 2);
        var unansweredSignIn = await SeedAsync(Member, minutesAgo: 3, key: NotificationKeys.NewLogin);
        var foreign = await SeedAsync(OtherMember, minutesAgo: 1, readAt: _clock.UtcNow);
        await using var app = await StartAsync(AuthState.Member);

        var response = await SendAsync(app, "DELETE", NotificationRoutes.ClearRead);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        _store.Documents.Select(document => document.Id)
            .ShouldBe(new[] { unread.Id, unansweredSignIn.Id, foreign.Id }, ignoreOrder: true);
    }

    [Fact]
    public async Task ClearRead_WhenNothingIsRead_StillGetsNoContent()
    {
        await SeedAsync(Member, minutesAgo: 1);
        await using var app = await StartAsync(AuthState.Member);

        var response = await SendAsync(app, "DELETE", NotificationRoutes.ClearRead);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        _store.Documents.ShouldHaveSingleItem();
    }

    #endregion

    #region Private Methods

    private static string ItemPath(string id)
    {
        return NotificationRoutes.Item.Replace("{id}", id);
    }

    private static string ReadPath(string id)
    {
        return NotificationRoutes.Read.Replace("{id}", id);
    }

    private static async Task<HttpResponseMessage> SendAsync(TestApp app, string method, string path)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);

        return await app.Client.SendAsync(request);
    }

    // Stores a notification created the given number of minutes ago; without an id it gets the next free one
    private async Task<InboxNotification> SeedAsync(
        string recipientId,
        int minutesAgo,
        DateTimeOffset? readAt = null,
        string? id = null,
        string key = Key)
    {
        var notification = new InboxNotification
        {
            Id = id ?? (++_lastId + 1000).ToString("x32"),
            RecipientId = recipientId,
            Key = key,
            Data = new Dictionary<string, string> { ["orderNumber"] = "1001" },
            CreatedAt = _clock.UtcNow.AddMinutes(-minutesAgo),
            ExpiresAt = _clock.UtcNow.AddDays(RetentionDays),
            ReadAt = readAt
        };

        (await _store.TryCreateAsync(notification)).ShouldBeTrue();

        return notification;
    }

    private DateTimeOffset? ReadAtOf(string id)
    {
        return _store.Documents.Single(document => document.Id == id).ReadAt;
    }

    // Builds the endpoints on a test server with the real service over the in-memory store
    private async Task<TestApp> StartAsync(AuthState authState)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<ICurrentUserContext>(new CurrentUserContext
        {
            IsAuthenticated = authState != AuthState.Anonymous,
            UserId = authState == AuthState.Anonymous ? null : Member,
            AuthState = authState
        });
        builder.Services.AddSingleton<IInboxService>(new InboxService(_store, _clock));

        var app = builder.Build();
        NotificationEndpoints.Map(app);
        await app.StartAsync();

        var server = (TestServer)app.Services.GetRequiredService<IServer>();

        return new TestApp(app, server.CreateClient());
    }

    #endregion

    #region Nested Types

    private sealed class TestApp(WebApplication app, HttpClient client) : IAsyncDisposable
    {
        public HttpClient Client { get; } = client;

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.DisposeAsync();
        }
    }

    #endregion
}
