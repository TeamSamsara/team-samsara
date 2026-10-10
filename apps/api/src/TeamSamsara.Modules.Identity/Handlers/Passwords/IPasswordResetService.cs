// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/IPasswordResetService.cs
// Version : 1.0.0
// Latest commit: feat/password-reset-service
// Author : Gerrah
// Purpose : Password reset for signed-out members: request a code, verify it, set a new password.

using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Handlers;

public interface IPasswordResetService
{
    #region Public Methods

    // Request a reset code by email.
    // The response is identical whether or not an account exists.
    public Task<PasswordCodeResult> RequestResetAsync(
        string email,
        CancellationToken cancellationToken = default
    );

    // Verifies the reset code and returns a one-time reset token.
    public Task<PasswordResetTokenResult> VerifyCodeAsync(
        string email,
        string code,
        CancellationToken cancellationToken = default
    );

    // Sets a new password using a reset token.
    // Invalid password does not consume token.
    public Task<PasswordStatus> SetNewPasswordAsync(
        string resetToken,
        string newPassword,
        CancellationToken cancellationToken = default
    );
    #endregion
}
