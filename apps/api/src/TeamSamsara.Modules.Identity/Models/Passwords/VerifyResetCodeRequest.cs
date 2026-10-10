// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/Passwords/VerifyResetCodeRequest.cs
// Version : 1.0.0
// Latest commit: feat/password-reset-endpoints
// Author : Gerrah
// Purpose : The body of a "check my reset code" request: the account's email and the code that
// was emailed to it.

namespace TeamSamsara.Modules.Identity.Models;

public record VerifyResetCodeRequest(string Email, string Code);
