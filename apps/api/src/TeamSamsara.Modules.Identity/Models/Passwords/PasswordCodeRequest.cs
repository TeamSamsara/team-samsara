// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/Passwords/PasswordCodeRequest.cs
// Version : 1.0.0
// Latest commit: feat/password-change-endpoints
// Author : Gerrah
// Purpose : The body of a "send me a password change code" request: the password the member
// wants, so it can be checked against the rules before a code is sent.

namespace TeamSamsara.Modules.Identity.Models;

public record PasswordCodeRequest(string NewPassword);
