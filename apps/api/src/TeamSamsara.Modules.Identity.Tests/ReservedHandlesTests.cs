// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/ReservedHandlesTests.cs
// Version : 1.0.0
// Latest commit: task/api-profile-onboarding
// Author : Gerrah
// Purpose : Proves the reserved handle list: team, staff and site-page names are refused, ordinary handles are not.

using Shouldly;
using TeamSamsara.Modules.Identity.Handlers;

namespace TeamSamsara.Modules.Identity.Tests;

public class ReservedHandlesTests
{
    #region Public Methods

    [Theory]
    [InlineData("admin")]
    [InlineData("support")]
    [InlineData("samsara")]
    [InlineData("teamsamsara")]
    [InlineData("member")]
    [InlineData("login")]
    public void AReservedHandle_IsRefused(string handle)
    {
        ReservedHandles.IsReserved(handle).ShouldBeTrue();
    }

    [Theory]
    [InlineData("gerrah")]
    [InlineData("member1234")]
    [InlineData("samsara_fan")]
    public void AnOrdinaryHandle_IsAllowed(string handle)
    {
        ReservedHandles.IsReserved(handle).ShouldBeFalse();
    }

    #endregion
}
