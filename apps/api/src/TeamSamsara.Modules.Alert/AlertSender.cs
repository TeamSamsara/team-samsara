// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Alert/AlertSender.cs
// Version : 1.0.0
// Latest commit: feature/alerts-module
// Author : Gerrah
// Purpose : IAlertSender implementation. Turns an alert request into a finished email via the
// alert templates and hands it to the generic email channel for delivery.

using TeamSamsara.Shared.Alert;
using TeamSamsara.Shared.Email;

namespace TeamSamsara.Modules.Alert;

public class AlertSender : IAlertSender
{
    #region Fields

    private readonly IEmailSender _emailSender;

    #endregion

    #region Constructors

    public AlertSender(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    #endregion

    #region Public Methods

    public Task SendAlertAsync(
        string to,
        AlertType type,
        IReadOnlyDictionary<string, string> templateData,
        CancellationToken cancellationToken = default)
    {
        var (subject, html) = AlertTemplates.Resolve(type, templateData);

        return _emailSender.SendEmailAsync(to, subject, html, cancellationToken);
    }

    #endregion
}
