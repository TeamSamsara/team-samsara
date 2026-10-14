// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Contact/Handlers/IContactService.cs
// Version : 1.0.0
// Latest commit: feat/contact-module
// Author : Gerrah
// Purpose : Contract for handling one contact form submission from validation to delivery.

using TeamSamsara.Modules.Contact.Models;

namespace TeamSamsara.Modules.Contact.Handlers;

public interface IContactService
{
    #region Public Methods

    // Validates the submission and emails it to the team, returning the outcome
    public Task<ContactStatus> SubmitAsync(ContactSubmission submission, CancellationToken cancellationToken);

    #endregion
}
