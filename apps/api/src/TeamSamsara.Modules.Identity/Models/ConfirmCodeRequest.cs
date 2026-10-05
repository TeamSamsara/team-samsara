// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/ConfirmCodeRequest.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : The body of every "confirm a code" request (registration and login challenge).

namespace TeamSamsara.Modules.Identity.Models;

public record ConfirmCodeRequest(string Code);
