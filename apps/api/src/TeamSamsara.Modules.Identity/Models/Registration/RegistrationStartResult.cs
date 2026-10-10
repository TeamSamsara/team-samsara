// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/RegistrationStartResult.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : What starting (or restarting) registration returns: the outcome, plus the code
// length and resend wait the client needs to draw its verification screen.

namespace TeamSamsara.Modules.Identity.Models;

public record RegistrationStartResult(
    RegistrationStatus Status,
    int CodeLength,
    int RetryAfterSeconds);
