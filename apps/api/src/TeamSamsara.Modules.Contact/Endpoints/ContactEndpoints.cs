// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Contact/Endpoints/ContactEndpoints.cs
// Version : 1.0.0
// Latest commit: feat/contact-module
// Author : Gerrah
// Purpose : The public, rate-limited HTTP endpoint that receives the contact form and returns the outcome.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TeamSamsara.Modules.Contact.Handlers;
using TeamSamsara.Modules.Contact.Models;

namespace TeamSamsara.Modules.Contact.Endpoints;

public static class ContactEndpoints
{
    #region Public Methods

    // Maps the contact form endpoint onto the route builder, open to anyone but limited per IP address
    public static void Map(IEndpointRouteBuilder routes)
    {
        var group = routes
            .MapGroup(ContactConstants.Route)
            .AllowAnonymous()
            .RequireRateLimiting(ContactConstants.RateLimitPolicyName);

        group.MapPost(string.Empty, SubmitAsync);
    }

    #endregion

    #region Private Methods

    // Reads the multipart form, hands it to the service and answers with the resulting status
    private static async Task<IResult> SubmitAsync(
        HttpRequest request,
        IContactService contact,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return Respond(ContactStatus.InvalidRequest);
        }

        IFormCollection form;
        try
        {
            form = await request.ReadFormAsync(cancellationToken);
        }
        catch (BadHttpRequestException exception) when (exception.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            return Respond(ContactStatus.FileTooLarge);
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException)
        {
            // BadHttpRequestException is an IOException, so a body that is cut short or not valid multipart ends here too.
            return Respond(ContactStatus.InvalidRequest);
        }

        var submission = new ContactSubmission
        {
            FirstName = form[ContactConstants.FirstNameField],
            LastName = form[ContactConstants.LastNameField],
            Email = form[ContactConstants.EmailField],
            Phone = form[ContactConstants.PhoneField],
            Category = form[ContactConstants.CategoryField],
            Message = form[ContactConstants.MessageField],
            Honeypot = form[ContactConstants.HoneypotField],
            Files = await ReadFilesAsync(form, cancellationToken)
        };

        var status = await contact.SubmitAsync(submission, cancellationToken);

        return Respond(status);
    }

    // Copies each uploaded file into memory so it can be attached to the email
    private static async Task<IReadOnlyList<ContactFile>> ReadFilesAsync(IFormCollection form, CancellationToken cancellationToken)
    {
        var files = new List<ContactFile>();

        foreach (var upload in form.Files.GetFiles(ContactConstants.FilesField))
        {
            await using var stream = upload.OpenReadStream();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);

            files.Add(new ContactFile(upload.FileName, buffer.ToArray()));
        }

        return files;
    }

    // Wraps the status in the JSON body with the HTTP code that matches it
    private static IResult Respond(ContactStatus status)
    {
        return Results.Json(new ContactResponse(status), statusCode: StatusCodeFor(status));
    }

    // Maps a contact status to its HTTP status code
    private static int StatusCodeFor(ContactStatus status)
    {
        return status switch
        {
            ContactStatus.Sent => StatusCodes.Status200OK,
            ContactStatus.FileTooLarge => StatusCodes.Status413PayloadTooLarge,
            ContactStatus.DeliveryFailed => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status400BadRequest
        };
    }

    #endregion
}
