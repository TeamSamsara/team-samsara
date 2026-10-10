// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Results/ErrorCodes.cs
// Version : 1.0.3
// Latest commit: feat/account-purge
// Author : Gerrah
// Purpose : Error.Code values.

namespace TeamSamsara.Shared.Results;

public static class ErrorCodes
{
    #region Fields

    public const string UnexpectedError = "unexpected_error";
    public const string AssetMetadataCreationFailed = "asset_metadata_creation_failed";
    public const string AssetMetadataDeletionFailed = "asset_metadata_deletion_failed";
    public const string AssetFileDeletionFailed = "asset_file_deletion_failed";

    #endregion
}
