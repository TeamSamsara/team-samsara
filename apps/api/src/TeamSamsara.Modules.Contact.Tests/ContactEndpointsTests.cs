// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Contact.Tests/ContactEndpointsTests.cs
// Version : 1.0.0
// Latest commit: feat/contact-module
// Author : Gerrah
// Purpose : Proves the contact endpoint reads the multipart form, maps each outcome to its HTTP status and JSON body, and limits each IP address.

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TeamSamsara.Modules.Contact.Endpoints;
using TeamSamsara.Modules.Contact.Handlers;
using TeamSamsara.Modules.Contact.Models;
using TeamSamsara.Shared.Http;
using TeamSamsara.Shared.Serialization;

namespace TeamSamsara.Modules.Contact.Tests;

public class ContactEndpointsTests
{
    #region Fields

    private const string ClientIpHeader = "X-Test-Ip";
    private const string OversizeHeader = "X-Test-Oversize";
    private const string FirstIp = "10.0.0.1";
    private const string SecondIp = "10.0.0.2";
    private const string MultipartBoundary = "test-boundary";
    private const int PermitLimit = 3;
    private const int WindowSeconds = 600;

    private readonly StubContactService _contact = new();

    #endregion

    #region Public Methods

    [Fact]
    public async Task AValidForm_GetsOkWithTheSentStatus()
    {
        await using var app = await StartAsync();

        var response = await PostAsync(app, CreateForm());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await StatusOf(response)).ShouldBe("sent");
    }

    [Fact]
    public async Task TheFormFields_ReachTheService()
    {
        await using var app = await StartAsync();
        var form = CreateForm();
        form.Add(new StringContent("spam"), ContactConstants.HoneypotField);

        await PostAsync(app, form);

        var submission = _contact.Received.ShouldHaveSingleItem();
        submission.FirstName.ShouldBe("Ada");
        submission.LastName.ShouldBe("Lovelace");
        submission.Email.ShouldBe("ada@example.com");
        submission.Phone.ShouldBe("5145550123");
        submission.Category.ShouldBe(ContactConstants.MediaValue);
        submission.Message.ShouldBe("Hello team");
        submission.Honeypot.ShouldBe("spam");
    }

    [Fact]
    public async Task TheUploadedFiles_ReachTheServiceWithTheirNamesAndContent()
    {
        await using var app = await StartAsync();
        var form = CreateForm();
        AddFile(form, "first.pdf", [1, 2, 3]);
        AddFile(form, "second.png", [4, 5]);

        await PostAsync(app, form);

        var files = _contact.Received.ShouldHaveSingleItem().Files;
        files.Count.ShouldBe(2);
        files[0].FileName.ShouldBe("first.pdf");
        files[0].Content.ShouldBe(new byte[] { 1, 2, 3 });
        files[1].FileName.ShouldBe("second.png");
        files[1].Content.ShouldBe(new byte[] { 4, 5 });
    }

    [Fact]
    public async Task AFormWithoutFiles_ReachesTheServiceWithNone()
    {
        await using var app = await StartAsync();

        await PostAsync(app, CreateForm());

        _contact.Received.ShouldHaveSingleItem().Files.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(ContactStatus.Sent, HttpStatusCode.OK, "sent")]
    [InlineData(ContactStatus.InvalidRequest, HttpStatusCode.BadRequest, "invalidRequest")]
    [InlineData(ContactStatus.InvalidField, HttpStatusCode.BadRequest, "invalidField")]
    [InlineData(ContactStatus.TooManyFiles, HttpStatusCode.BadRequest, "tooManyFiles")]
    [InlineData(ContactStatus.UnsupportedFileType, HttpStatusCode.BadRequest, "unsupportedFileType")]
    [InlineData(ContactStatus.FileTooLarge, HttpStatusCode.RequestEntityTooLarge, "fileTooLarge")]
    [InlineData(ContactStatus.DeliveryFailed, HttpStatusCode.ServiceUnavailable, "deliveryFailed")]
    public async Task EachStatus_MapsToItsHttpCodeAndJsonBody(ContactStatus status, HttpStatusCode expectedCode, string expectedBody)
    {
        _contact.Result = status;
        await using var app = await StartAsync();

        var response = await PostAsync(app, CreateForm());

        response.StatusCode.ShouldBe(expectedCode);
        (await StatusOf(response)).ShouldBe(expectedBody);
    }

    [Fact]
    public async Task ABodyThatIsNotAForm_GetsBadRequest_AndTheServiceIsNotCalled()
    {
        await using var app = await StartAsync();
        var body = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await PostAsync(app, body);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await StatusOf(response)).ShouldBe("invalidRequest");
        _contact.Received.ShouldBeEmpty();
    }

    [Fact]
    public async Task NoBody_GetsBadRequest()
    {
        await using var app = await StartAsync();

        var response = await PostAsync(app, content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _contact.Received.ShouldBeEmpty();
    }

    [Fact]
    public async Task AMalformedMultipartBody_GetsBadRequest()
    {
        await using var app = await StartAsync();
        var body = new StringContent("this is not a multipart body", Encoding.UTF8);
        body.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("multipart/form-data")
        {
            Parameters = { new System.Net.Http.Headers.NameValueHeaderValue("boundary", MultipartBoundary) }
        };

        var response = await PostAsync(app, body);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _contact.Received.ShouldBeEmpty();
    }

    [Fact]
    public async Task ABodyOverTheServerLimit_GetsPayloadTooLarge()
    {
        await using var app = await StartAsync();

        var response = await PostAsync(app, CreateForm(), extraHeader: OversizeHeader);

        response.StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
        (await StatusOf(response)).ShouldBe("fileTooLarge");
        _contact.Received.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnIpAddressOverTheLimit_GetsTooManyRequests_AndOthersAreUnaffected()
    {
        await using var app = await StartAsync();

        for (var attempt = 0; attempt < PermitLimit; attempt++)
        {
            (await PostAsync(app, CreateForm(), FirstIp)).StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        (await PostAsync(app, CreateForm(), FirstIp)).StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        (await PostAsync(app, CreateForm(), SecondIp)).StatusCode.ShouldBe(HttpStatusCode.OK);
        _contact.Received.Count.ShouldBe(PermitLimit + 1);
    }

    #endregion

    #region Private Methods

    // A form with every text field a visitor fills in
    private static MultipartFormDataContent CreateForm()
    {
        return new MultipartFormDataContent(MultipartBoundary)
        {
            { new StringContent("Ada"), ContactConstants.FirstNameField },
            { new StringContent("Lovelace"), ContactConstants.LastNameField },
            { new StringContent("ada@example.com"), ContactConstants.EmailField },
            { new StringContent("5145550123"), ContactConstants.PhoneField },
            { new StringContent(ContactConstants.MediaValue), ContactConstants.CategoryField },
            { new StringContent("Hello team"), ContactConstants.MessageField }
        };
    }

    // Adds one uploaded file to the form
    private static void AddFile(MultipartFormDataContent form, string fileName, byte[] content)
    {
        form.Add(new ByteArrayContent(content), ContactConstants.FilesField, fileName);
    }

    // Posts to the contact route as the given client, optionally flagging the body as over the server limit
    private static async Task<HttpResponseMessage> PostAsync(
        TestApp app,
        HttpContent? content,
        string ip = FirstIp,
        string? extraHeader = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, ContactConstants.Route) { Content = content };
        request.Headers.Add(ClientIpHeader, ip);

        if (extraHeader is not null)
        {
            request.Headers.Add(extraHeader, "true");
        }

        return await app.Client.SendAsync(request);
    }

    // Reads the status string out of the JSON response
    private static async Task<string?> StatusOf(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.GetProperty("status").GetString();
    }

    // Starts the endpoint on a test server with the shared JSON convention and the per-IP limit
    private async Task<TestApp> StartAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IContactService>(_contact);
        builder.Services.AddSamsaraRateLimiting(new RateLimitSettings { PermitLimit = PermitLimit });
        builder.Services.AddPerIpRateLimitPolicy(ContactConstants.RateLimitPolicyName, PermitLimit, WindowSeconds);
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = DefaultJsonOptions.Instance.PropertyNamingPolicy;

            foreach (var converter in DefaultJsonOptions.Instance.Converters)
            {
                options.SerializerOptions.Converters.Add(converter);
            }
        });

        var app = builder.Build();

        // The test server has no remote address, so the tests choose one per request.
        app.Use((context, next) =>
        {
            if (context.Request.Headers.TryGetValue(ClientIpHeader, out var ip))
            {
                context.Connection.RemoteIpAddress = IPAddress.Parse(ip.ToString());
            }

            // Stands in for the server refusing a body over its size limit, which the test server never does.
            if (context.Request.Headers.ContainsKey(OversizeHeader))
            {
                context.Request.Body = new OversizeBodyStream();
            }

            return next();
        });

        app.UseRouting();
        app.UseRateLimiter();
        ContactEndpoints.Map(app);
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

    // Answers with a configured status and records every submission it was given
    private sealed class StubContactService : IContactService
    {
        public ContactStatus Result { get; set; } = ContactStatus.Sent;

        public List<ContactSubmission> Received { get; } = new();

        public Task<ContactStatus> SubmitAsync(ContactSubmission submission, CancellationToken cancellationToken)
        {
            Received.Add(submission);

            return Task.FromResult(Result);
        }
    }

    // A request body that fails the way the server does when a client sends more than the limit allows
    private sealed class OversizeBodyStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new BadHttpRequestException("Request body too large.", StatusCodes.Status413PayloadTooLarge);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            throw new BadHttpRequestException("Request body too large.", StatusCodes.Status413PayloadTooLarge);
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    #endregion
}
