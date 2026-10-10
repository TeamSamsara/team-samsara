// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Endpoints/IdentityRoutes.cs
// Version : 1.3.0
// Latest commit: feat/password-change-endpoints
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

    public const string Me = "/me";

    public const string ProfileGroup = "/profile";
    public const string ProfileRoot = "/";
    public const string ProfilePicture = "/picture";
    public const string ProfileBanner = "/banner";

    public const string PasswordGroup = "/password";
    public const string PasswordRequestCode = "/request-code";
    public const string PasswordChange = "/change";

    #endregion
}
