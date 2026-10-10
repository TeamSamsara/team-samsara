// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/ProfileEndpointsTests.cs
// Version : 1.0.0
// Latest commit: fix/profile-guest-before-form
// Author : Gerrah
// Purpose : Proves the profile image endpoints refuse non-members before reading the request body,
// and reject malformed uploads from members.

using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using TeamSamsara.Modules.Identity.Endpoints;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Tests.Fakes;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Tests;

public class ProfileEndpointsTests
{
    #region Fields

    private const string UserId = "user-1";
    private const string PicturePath = "/profile/picture";
    private const string BannerPath = "/profile/banner";
    private const string FileField = "file";
    private const string OtherField = "not-the-file";
    private const string ImageContentType = "image/png";

    private readonly InMemoryProfileStore _profiles = new();
    private readonly FakeMemberImageService _images = new();

    #endregion

    #region Public Methods

    [Theory]
    [InlineData(PicturePath)]
    [InlineData(BannerPath)]
    public async Task AnAnonymousCaller_GetsUnauthorized_EvenWithoutAFormBody(string path)
    {
        await using var app = await StartAsync(AuthState.Anonymous);

        var response = await app.Client.PutAsync(path, content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(PicturePath, AuthState.RegistrationIncomplete)]
    [InlineData(PicturePath, AuthState.VerificationRequired)]
    [InlineData(BannerPath, AuthState.RegistrationIncomplete)]
    [InlineData(BannerPath, AuthState.VerificationRequired)]
    public async Task AnUnverifiedCaller_GetsForbidden_EvenWithoutAFormBody(
        string path, AuthState authState)
    {
        await using var app = await StartAsync(authState);

        var response = await app.Client.PutAsync(path, content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        _images.StoredAssetIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task AMember_SendingNoForm_GetsBadRequest()
    {
        await using var app = await StartAsync(AuthState.Member);

        var response = await app.Client.PutAsync(PicturePath, content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _images.StoredAssetIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task AMember_SendingAFormWithoutTheFile_GetsBadRequest()
    {
        await using var app = await StartAsync(AuthState.Member);

        var response = await app.Client.PutAsync(PicturePath, BuildForm(OtherField));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _images.StoredAssetIds.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(PicturePath)]
    [InlineData(BannerPath)]
    public async Task AMember_SendingAFile_StoresTheImage(string path)
    {
        await using var app = await StartAsync(AuthState.Member);

        var response = await app.Client.PutAsync(path, BuildForm(FileField));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _images.StoredAssetIds.Count.ShouldBe(1);
    }

    #endregion

    #region Private Methods

    private static MultipartFormDataContent BuildForm(string fieldName)
    {
        var file = new ByteArrayContent(new byte[] { 1, 2, 3 });
        file.Headers.ContentType = new MediaTypeHeaderValue(ImageContentType);

        return new MultipartFormDataContent { { file, fieldName, "image.png" } };
    }

    private async Task<TestApp> StartAsync(AuthState authState)
    {
        await _profiles.CreateAsync(new Profile { Id = UserId, DisplayName = "Member" });

        var settings = new ProfileSettings
        {
            DisplayNameMinLength = 2,
            DisplayNameMaxLength = 10,
            BioMaxLength = 20
        };

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<ICurrentUserContext>(new CurrentUserContext
        {
            IsAuthenticated = authState != AuthState.Anonymous,
            UserId = authState == AuthState.Anonymous ? null : UserId,
            AuthState = authState
        });
        builder.Services.AddSingleton<IProfileService>(new ProfileService(
            _profiles, _images, Options.Create(settings), NullLogger<ProfileService>.Instance));

        var app = builder.Build();
        ProfileEndpoints.Map(app);
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
