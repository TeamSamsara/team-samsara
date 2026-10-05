// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/Fakes/FakeClock.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : A clock tests can move forward, so expiry and cooldown rules can be checked
// without waiting.

using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Tests.Fakes;

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
