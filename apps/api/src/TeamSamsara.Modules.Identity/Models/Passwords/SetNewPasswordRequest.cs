// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/Passwords/SetNewPasswordRequest.cs
// Version : 1.0.0
// Latest commit: feat/password-reset-endpoints
// Author : Gerrah
// Purpose : The body of a "set my new password" request: the one-time reset token and the
// password to set.

namespace TeamSamsara.Modules.Identity.Models;

public record SetNewPasswordRequest(string ResetToken, string NewPassword);
