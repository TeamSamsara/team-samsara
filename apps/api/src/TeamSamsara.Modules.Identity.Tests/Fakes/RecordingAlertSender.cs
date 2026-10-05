// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/Fakes/RecordingAlertSender.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : An alert sender that records what it was asked to send instead of emailing it, so
// tests can read the verification code a member would have received.

using TeamSamsara.Shared.Alert;

namespace TeamSamsara.Modules.Identity.Tests.Fakes;

public class RecordingAlertSender : IAlertSender
{
    #region Properties

    // Everything sent, oldest first
    public List<SentAlert> Sent { get; } = new();

    // When true, sending throws (to test what happens when email delivery fails)
    public bool ShouldFail { get; set; }

    // Alert types that fail to send while others succeed
    public HashSet<AlertType> FailingTypes { get; } = new();

    // The code in the most recent alert that carried one
    public string LastCode => Sent.Last(alert => alert.TemplateData.ContainsKey(AlertTemplateKeys.Code))
        .TemplateData[AlertTemplateKeys.Code];

    #endregion

    #region Public Methods

    public Task SendAlertAsync(
        string to,
        AlertType type,
        IReadOnlyDictionary<string, string> templateData,
        CancellationToken cancellationToken = default)
    {
        if (ShouldFail || FailingTypes.Contains(type))
        {
            throw new InvalidOperationException("Simulated email failure.");
        }

        Sent.Add(new SentAlert(to, type, new Dictionary<string, string>(templateData)));

        return Task.CompletedTask;
    }

    #endregion
}

public record SentAlert(string To, AlertType Type, IReadOnlyDictionary<string, string> TemplateData);
