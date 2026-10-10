// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/Passwords/ChangePasswordRequest.cs
// Version : 1.0.0
// Latest commit: feat/password-change-endpoints
// Author : Gerrah
// Purpose : The body of a "change my password" request: the new password and the code that was
// emailed to the member.

namespace TeamSamsara.Modules.Identity.Models;

public record ChangePasswordRequest(string NewPassword, string Code);
