// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/Passwords/PasswordResetTokenResult.cs
// Version : 1.0.0
// Latest commit: feat/password-reset-service
// Author : Gerrah
// Purpose : Result of verifying a reset code: the outcome and, on success, the reset token.

namespace TeamSamsara.Modules.Identity.Models;

public record PasswordResetTokenResult(PasswordStatus Status, string? ResetToken);
