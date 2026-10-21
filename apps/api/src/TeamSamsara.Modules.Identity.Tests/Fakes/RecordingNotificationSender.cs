// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/Fakes/RecordingNotificationSender.cs
// Version : 1.0.0
// Latest commit: task/notifications-api
// Author : Gerrah
// Purpose : A notification sender that records every request it receives, and can report a status or throw instead, so tests can check what a member would be told and that a failure never breaks the caller.

using TeamSamsara.Shared.Notification;

namespace TeamSamsara.Modules.Identity.Tests.Fakes;

public class RecordingNotificationSender : INotificationSender
{
    #region Properties

    // Every request received, including the ones that then failed
    public List<NotificationRequest> Requests { get; } = new();

    // Thrown by every send when set
    public Exception? FailureToThrow { get; set; }

    // The status every send reports when it does not throw
    public NotificationStatus StatusToReturn { get; set; } = NotificationStatus.Stored;

    #endregion

    #region Public Methods

    public Task<NotificationResult> SendNotificationAsync(
        NotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        Requests.Add(request);

        if (FailureToThrow is not null)
        {
            throw FailureToThrow;
        }

        var id = StatusToReturn == NotificationStatus.Stored ? "notification-id" : null;

        return Task.FromResult(new NotificationResult(StatusToReturn, id));
    }

    #endregion
}
