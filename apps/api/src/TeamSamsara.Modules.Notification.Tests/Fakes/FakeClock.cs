// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification.Tests/Fakes/FakeClock.cs
// Version : 1.0.0
// Latest commit: feat/notification-send-path
// Author : Gerrah
// Purpose : A clock tests can set and move forward, so stored timestamps can be checked exactly.

using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Notification.Tests.Fakes;

public class FakeClock : IClock
{
    #region Properties

    public DateTimeOffset UtcNow { get; set; } = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    #endregion

    #region Public Methods

    // Moves the clock forward
    public void Advance(TimeSpan amount)
    {
        UtcNow += amount;
    }

    #endregion
}
