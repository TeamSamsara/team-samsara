// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Repositories/IdentityFirestoreCollections.cs
// Version : 1.2.0
// Latest commit: feat/password-reset-token-store
// Author : Gerrah
// Purpose : Firestore collection names owned by the Identity module.

namespace TeamSamsara.Modules.Identity.Repositories;

public static class IdentityFirestoreCollections
{
    #region Fields

    // Security/identity records, one per account, keyed by Firebase uid. Never user-editable.
    public const string Users = "users";

    // Display/personalization records, one per member, keyed by Firebase uid. User-editable.
    public const string Profiles = "profiles";

    // Pending verification codes, keyed by "{uid}_{purpose}". Short-lived.
    public const string VerificationCodes = "verificationCodes";

    // Pending password reset tokens, keyed by the hash of the token. Short-lived, single use.
    public const string PasswordResetTokens = "passwordResetTokens";

    #endregion
}
