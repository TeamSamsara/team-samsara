// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/EmailChange/ChangeEmailRequest.cs
// Version : 1.0.0
// Latest commit: feat/email-change-endpoints
// Author : Gerrah
// Purpose : The body of a "change my email" request: the address the member wants.

namespace TeamSamsara.Modules.Identity.Models;

public record ChangeEmailRequest(string NewEmail);
