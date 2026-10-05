// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/LoginResult.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : What a login check (or challenge resend) returns: the outcome, plus the code
// length and resend wait the client needs to draw its challenge screen. Both are 0 when no
// challenge is involved.

namespace TeamSamsara.Modules.Identity.Models;

public record LoginResult(
    AuthenticationStatus Status,
    int CodeLength,
    int RetryAfterSeconds);
