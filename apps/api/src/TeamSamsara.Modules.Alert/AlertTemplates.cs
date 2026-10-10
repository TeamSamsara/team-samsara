// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Alert/AlertTemplates.cs
// Version : 1.3.0
// Latest commit: feat/account-deletion
// Author : Gerrah
// Purpose : Resolves an AlertType into its email subject and HTML body.

using System.Net;
using System.Text;
using TeamSamsara.Shared.Alert;

namespace TeamSamsara.Modules.Alert;

internal static class AlertTemplates
{
    #region Public Methods

    // Picks the subject and body for the type and fills in its placeholders
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

            AlertType.EmailChanged =>
                (AlertMessages.EmailChangedSubject, AlertMessages.EmailChangedBody),

            AlertType.AccountDeleted =>
                (AlertMessages.AccountDeletedSubject, AlertMessages.AccountDeletedBody),

            _ => throw new ArgumentOutOfRangeException(
                nameof(type), type, "No template defined for this alert type.")
        };

        return (subject, Substitute(html, templateData));
    }

    #endregion

    #region Private Methods

    // Replaces each {{token}} with its HTML-encoded value, since some values come from the requester
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
