// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Endpoints/IdentityRoutes.cs
// Version : 1.8.0
// Latest commit: task/api-profile-onboarding
// Author : Gerrah
// Purpose : Route paths of the Identity module's endpoints, shared between the endpoint
// classes and their tests.

namespace TeamSamsara.Modules.Identity.Endpoints;

public static class IdentityRoutes
{
    #region Fields

    public const string Group = "/account";

    public const string Register = "/register";
    public const string RegisterResend = "/register/resend";
    public const string RegisterConfirm = "/register/confirm";

    public const string LoginCheck = "/login/check";
    public const string LoginConfirm = "/login/confirm";
    public const string LoginResend = "/login/resend";

    public const string Logout = "/logout";
    public const string LogoutAll = "/logout/all";

    public const string Me = "/me";

    public const string ProfileGroup = "/profile";
    public const string ProfileRoot = "/";
    public const string ProfilePicture = "/picture";
    public const string ProfileBanner = "/banner";
    public const string ProfileHandle = "/handle";
    public const string ProfileHandleAvailable = "/handle/available";
    public const string ProfileOnboardingComplete = "/onboarding/complete";

    // Anyone's public profile, mapped outside the signed-in group, so it carries the full path
    public const string PublicProfileGroup = "/profiles";
    public const string PublicProfileByHandle = "/{handle}";

    public const string PasswordGroup = "/password";
    public const string PasswordRequestCode = "/request-code";
    public const string PasswordChange = "/change";

    // Signed-out reset: mapped outside the authorized group, so it carries the full path
    public const string PasswordResetGroup = Group + "/password/reset";
    public const string PasswordResetRequest = "/request";
    public const string PasswordResetVerify = "/verify";
    public const string PasswordResetConfirm = "/confirm";

    public const string EmailGroup = "/email";
    public const string EmailRequest = "/request";
    public const string EmailConfirm = "/confirm";

    public const string DeleteGroup = "/delete";
    public const string DeleteRequestCode = "/request-code";
    public const string DeleteConfirm = "/";

    #endregion
}
