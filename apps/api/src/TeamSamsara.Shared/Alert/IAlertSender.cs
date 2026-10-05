// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Alert/IAlertSender.cs
// Version : 1.0.0
// Latest commit: feature/alerts-module
// Author : Gerrah
// Purpose : Contract any module uses to request a system alert.

namespace TeamSamsara.Shared.Alert;

public interface IAlertSender
{
    #region  Public Methods

    // Sends the alert identified by 'type' to 'to', rendering 'templateData' into that alerts template.
    // 'To' represents the recipient Address
    public Task SendAlertAsync(string to, AlertType type, IReadOnlyDictionary<string, string> templateData, CancellationToken cancellationToken = default);

    #endregion
}
