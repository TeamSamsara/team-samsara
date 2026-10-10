// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/Passwords/PasswordResetRequest.cs
// Version : 1.0.0
// Latest commit: feat/password-reset-endpoints
// Author : Gerrah
// Purpose : The body of a "send me a password reset code" request: the account's email.

namespace TeamSamsara.Modules.Identity.Models;

public record PasswordResetRequest(string Email);
