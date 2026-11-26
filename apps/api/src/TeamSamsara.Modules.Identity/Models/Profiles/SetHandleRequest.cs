// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/Profiles/SetHandleRequest.cs
// Version : 1.0.0
// Latest commit: task/api-profile-onboarding
// Author : Gerrah
// Purpose : The body for changing a member's @handle. The service lowercases
// and validates it, so the request carries what the member typed.

namespace TeamSamsara.Modules.Identity.Models;

public record SetHandleRequest(string Handle);
