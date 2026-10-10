// File: /team-samsara/apps/api/src/TeamSamsara.Shared/Http/AuthStateResponse.cs
// Version: 1.0.0
// Latest commit: feature/identity-profile
// Author: Gerrah
//
// Purpose: The body of a 403 from a member-only endpoint, telling the client why it was refused.

using TeamSamsara.Shared.Context;

namespace TeamSamsara.Shared.Http;

public record AuthStateResponse(AuthState AuthState);
