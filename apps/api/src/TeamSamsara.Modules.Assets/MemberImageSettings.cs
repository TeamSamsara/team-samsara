// File: /team-samsara/apps/api/src/TeamSamsara.Modules.Assets/MemberImageSettings.cs
// Version: 1.0.0
// Latest commit: feature/identity-profile
// Author: Gerrah
//
// Purpose: Size limits for the images members upload to their profile.

using System.ComponentModel.DataAnnotations;

namespace TeamSamsara.Modules.Assets;

public class MemberImageSettings
{
    #region Fields

    public const string SectionName = "MemberImages";

    #endregion

    #region Properties

    // Largest accepted profile picture, in bytes (default 2 MB)
    [Range(1, int.MaxValue)]
    public int ProfilePictureMaxBytes { get; set; } = 2 * 1024 * 1024;

    // Largest accepted banner, in bytes (default 5 MB)
    [Range(1, int.MaxValue)]
    public int BannerMaxBytes { get; set; } = 5 * 1024 * 1024;

    #endregion
}
