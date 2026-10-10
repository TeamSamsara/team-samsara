// File: /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/ProfileStatus.cs
// Version: 1.0.0
// Latest commit: feature/identity-profile
// Author: Gerrah
//
// Purpose: The outcome of a profile operation. Endpoints translate these into responses.

namespace TeamSamsara.Modules.Identity.Models;

public enum ProfileStatus
{
    // The operation succeeded
    Success,

    // The member has no profile
    ProfileNotFound,

    // The display name is empty, too short, too long, or contains disallowed characters
    InvalidDisplayName,

    // The bio is too long or contains disallowed characters
    InvalidBio,

    // The uploaded file is not a PNG, JPEG or WebP image
    ImageUnsupportedType,

    // The uploaded image is larger than the limit for this kind of image
    ImageTooLarge
}
