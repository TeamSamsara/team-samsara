// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/EmailChange/ConfirmEmailChangeRequest.cs
// Version : 1.1.0
// Latest commit: task/web-account-security
// Author : Gerrah
// Purpose : The body of a "confirm my email change" request: the code sent to the old address and the one sent to the new
// address. Either may be left out and sent in a later call.

namespace TeamSamsara.Modules.Identity.Models;

public record ConfirmEmailChangeRequest(string? OldCode, string? NewCode);
