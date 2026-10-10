// File: /team-samsara/apps/api/src/TeamSamsara.Shared/Assets/MemberImageStatus.cs
// Version: 1.0.0
// Latest commit: feature/identity-profile
// Author: Gerrah
//
// Purpose: The outcome of storing a member image. Callers translate these into responses.

namespace TeamSamsara.Shared.Assets;

public enum MemberImageStatus
{
    // The image was stored
    Success,

    // The file is not a PNG, JPEG or WebP image
    UnsupportedType,

    // The file is larger than the limit for this kind of image
    TooLarge
}
