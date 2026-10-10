// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Repositories/IdentityFirestoreCollections.cs
// Version : 1.3.0
// Latest commit: feat/email-change-request-store
// Author : Gerrah
// Purpose : Firestore collection names owned by the Identity module.

namespace TeamSamsara.Modules.Identity.Repositories;

public static class IdentityFirestoreCollections
{
    #region Fields

    // Security records, one per account, keyed by Firebase uid; never user-editable
    public const string Users = "users";

    // Display records, one per member, keyed by Firebase uid; user-editable
    public const string Profiles = "profiles";

    // Pending verification codes, keyed by "{uid}_{purpose}"; short-lived
    public const string VerificationCodes = "verificationCodes";

    // Pending password reset tokens, keyed by the token hash; short-lived, single use
    public const string PasswordResetTokens = "passwordResetTokens";

    // Pending email changes, keyed by uid; short-lived
    public const string EmailChangeRequests = "emailChangeRequests";

    #endregion
}
