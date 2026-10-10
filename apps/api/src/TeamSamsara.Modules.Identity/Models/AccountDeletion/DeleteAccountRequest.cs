// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/AccountDeletion/DeleteAccountRequest.cs
// Version : 1.0.0
// Latest commit: feat/account-deletion
// Author : Gerrah
// Purpose : The body of a "delete my account" request: the code that was emailed to the member.

namespace TeamSamsara.Modules.Identity.Models;

public record DeleteAccountRequest(string Code);
