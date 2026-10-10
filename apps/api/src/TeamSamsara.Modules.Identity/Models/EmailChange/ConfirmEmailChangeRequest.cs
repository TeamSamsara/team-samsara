// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/EmailChange/ConfirmEmailChangeRequest.cs
// Version : 1.0.0
// Latest commit: feat/email-change-endpoints
// Author : Gerrah
// Purpose : The body of a "confirm my email change" request: the codes sent to the old and new addresses.

namespace TeamSamsara.Modules.Identity.Models;

public record ConfirmEmailChangeRequest(string OldCode, string NewCode);
