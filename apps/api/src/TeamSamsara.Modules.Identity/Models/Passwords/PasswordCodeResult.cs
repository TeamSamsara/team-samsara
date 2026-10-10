// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/PasswordCodeResult.cs
// Version : 1.0.0
// Latest commit: feat/password-change-service
// Author : Gerrah
// Purpose : What requesting a password code returns: the outcome, plus the code length and
// resend wait the client needs to draw its verification screen.

namespace TeamSamsara.Modules.Identity.Models;

public record PasswordCodeResult(
    PasswordStatus Status,
    int CodeLength,
    int RetryAfterSeconds);
