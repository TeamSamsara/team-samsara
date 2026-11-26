// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/Profiles/ProfileStatus.cs
// Version : 1.1.0
// Latest commit: task/api-profile-onboarding
// Author : Gerrah
// Purpose : The outcome of a profile operation. Endpoints translate these into
// responses.

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
    ImageTooLarge,

    // The handle is too short, too long, or uses characters other than a-z, 0-9 and _
    InvalidHandle,

    // The handle belongs to another member or is on the reserved list
    HandleTaken,

    // The member changed their handle too recently to change it again
    HandleCooldown
}
