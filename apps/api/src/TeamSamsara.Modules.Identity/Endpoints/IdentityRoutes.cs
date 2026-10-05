// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Endpoints/IdentityRoutes.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
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

    #endregion
}
