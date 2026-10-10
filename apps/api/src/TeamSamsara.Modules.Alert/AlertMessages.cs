// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Alert/AlertMessages.cs
// Version : 1.2.0
// Latest commit: feat/email-change-foundation
// Author : Gerrah
// Purpose : Subject and body text for every alert type, with {{token}} placeholders filled from the caller's template data.

using TeamSamsara.Shared.Alert;

namespace TeamSamsara.Modules.Alert;

internal static class AlertMessages
{
    #region Fields

    public const string RegistrationCodeSubject = "Alert: registration code";
    public const string RegistrationCodeBody =
        "<p>Placeholder - registration code: {{" + AlertTemplateKeys.Code + "}}</p>";

    public const string StepUpCodeSubject = "Alert: confirmation code";
    public const string StepUpCodeBody =
        "<p>Placeholder - confirmation code: {{" + AlertTemplateKeys.Code + "}}</p>";

    public const string NewLoginAttemptSubject = "Alert: new login attempt";
    public const string NewLoginAttemptBody =
        "<p>Placeholder - new login attempt from IP {{" + AlertTemplateKeys.IpAddress +
        "}}, device {{" + AlertTemplateKeys.UserAgent + "}}</p>";

    public const string PasswordResetRequestedSubject = "Alert: password reset requested";
    public const string PasswordResetRequestedBody =
        "<p>Placeholder - password reset requested from IP {{" + AlertTemplateKeys.IpAddress +
        "}}, device {{" + AlertTemplateKeys.UserAgent + "}}</p>";

    public const string PasswordChangedSubject = "Alert: password changed";
    public const string PasswordChangedBody =
        "<p>Placeholder - your password was changed</p>";

    public const string EmailChangedSubject = "Alert: email changed";
    public const string EmailChangedBody =
        "<p>Placeholder - your email was changed to {{" + AlertTemplateKeys.NewEmail + "}}</p>";

    #endregion
}
