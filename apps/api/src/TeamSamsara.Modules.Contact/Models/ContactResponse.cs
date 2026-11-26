// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Contact/Models/ContactResponse.cs
// Version : 1.0.0
// Latest commit: feat/contact-module
// Author : Gerrah
// Purpose : The body returned by the contact endpoint: just the outcome. Serialized with the
// shared JSON convention, so the status reaches the client as a camelCase string.

namespace TeamSamsara.Modules.Contact.Models;

public record ContactResponse(ContactStatus Status);
