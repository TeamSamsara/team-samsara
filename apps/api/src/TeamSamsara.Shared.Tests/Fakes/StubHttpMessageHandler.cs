// File : /team-samsara/apps/api/src/TeamSamsara.Shared.Tests/Fakes/StubHttpMessageHandler.cs
// Version : 1.0.0
// Latest commit: feat/email-message-attachments
// Author : Gerrah
// Purpose : An HTTP handler that records the request it receives and answers with a fixed response.

using System.Net;

namespace TeamSamsara.Shared.Tests.Fakes;

public class StubHttpMessageHandler : HttpMessageHandler
{
    #region Properties

    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

    public string ResponseBody { get; set; } = "{}";

    public int RequestCount { get; private set; }

    public HttpMethod? Method { get; private set; }

    public Uri? RequestUri { get; private set; }

    public string? Body { get; private set; }

    #endregion

    #region Protected Methods

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestCount++;
        Method = request.Method;
        RequestUri = request.RequestUri;
        Body = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        return new HttpResponseMessage(StatusCode)
        {
            Content = new StringContent(ResponseBody)
        };
    }

    #endregion
}
