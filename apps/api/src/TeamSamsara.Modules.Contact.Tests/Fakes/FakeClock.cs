// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Contact.Tests/Fakes/FakeClock.cs
// Version : 1.0.0
// Latest commit: feat/contact-module
// Author : Gerrah
// Purpose : A clock tests can set, so the received time in the email can be checked exactly.

using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Contact.Tests.Fakes;

public class FakeClock : IClock
{
    #region Properties

    public DateTimeOffset UtcNow { get; set; } = new(2026, 1, 15, 22, 45, 36, TimeSpan.Zero);

    #endregion
}
