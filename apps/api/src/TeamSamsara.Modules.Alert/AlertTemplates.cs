// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Alert/AlertTemplates.cs
// Version : 1.1.0
// Latest commit: feat/alert-password-notices
// Author : Gerrah
// Purpose : Resolves an AlertType into its email subject and HTML body (text lives in
// AlertMessages), substituting {{token}} placeholders from the caller-supplied template data.
// Substituted values are HTML-encoded, since some (like the user agent) originate from the
// requester.

using System.Net;
using System.Text;
using TeamSamsara.Shared.Alert;

namespace TeamSamsara.Modules.Alert;

internal static class AlertTemplates
{
    #region Public Methods

    public static (string Subject, string Html) Resolve(
        AlertType type,
        IReadOnlyDictionary<string, string> templateData)
    {
        var (subject, html) = type switch
        {
            AlertType.RegistrationCode =>
                (AlertMessages.RegistrationCodeSubject, AlertMessages.RegistrationCodeBody),

            AlertType.StepUpCode =>
                (AlertMessages.StepUpCodeSubject, AlertMessages.StepUpCodeBody),

            AlertType.NewLoginAttempt =>
                (AlertMessages.NewLoginAttemptSubject, AlertMessages.NewLoginAttemptBody),

            AlertType.PasswordResetRequested =>
                (AlertMessages.PasswordResetRequestedSubject, AlertMessages.PasswordResetRequestedBody),

            AlertType.PasswordChanged =>
                (AlertMessages.PasswordChangedSubject, AlertMessages.PasswordChangedBody),

            _ => throw new ArgumentOutOfRangeException(
                nameof(type), type, "No template defined for this alert type.")
        };

        return (subject, Substitute(html, templateData));
    }

    #endregion

    #region Private Methods

    private static string Substitute(string html, IReadOnlyDictionary<string, string> templateData)
    {
        var builder = new StringBuilder(html);

        foreach (var (key, value) in templateData)
        {
            builder.Replace("{{" + key + "}}", WebUtility.HtmlEncode(value));
        }

        return builder.ToString();
    }

    #endregion
}
