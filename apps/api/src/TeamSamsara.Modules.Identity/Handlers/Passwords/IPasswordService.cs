// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/IPasswordService.cs
// Version : 1.0.0
// Latest commit: feat/password-change-service
// Author : Gerrah
// Purpose : Changes a signed-in member's password after they confirm a code sent to their
// email.

using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Handlers;

public interface IPasswordService
{
    #region Public Methods

    // Checks the new password against the rules, then sends the confirmation code. Nothing is
    // sent when the password is unacceptable, so the member finds out before using a code.
    public Task<PasswordCodeResult> RequestChangeCodeAsync(
        string userId,
        string newPassword,
        CancellationToken cancellationToken = default);

    // Checks the password rules, then the code, and if both pass replaces the password and signs
    // the member out everywhere. An unacceptable password never uses up the code.
    public Task<PasswordStatus> ChangeAsync(
        string userId,
        string newPassword,
        string code,
        CancellationToken cancellationToken = default);

    #endregion
}
