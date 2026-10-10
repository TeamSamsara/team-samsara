// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/EmailMaskingTests.cs
// Version : 1.0.0
// Latest commit: feat/email-change-service
// Author : Gerrah
// Purpose : Proves email masking keeps only the first character and the domain.

using Shouldly;
using TeamSamsara.Modules.Identity.Handlers;

namespace TeamSamsara.Modules.Identity.Tests;

public class EmailMaskingTests
{
    #region Public Methods

    [Fact]
    public void Mask_KeepsTheFirstCharacterAndTheDomain()
    {
        EmailMasking.Mask("jane.doe@example.com").ShouldBe("j***@example.com");
    }

    [Fact]
    public void Mask_DoesNotRevealTheLengthOfTheName()
    {
        EmailMasking.Mask("a@example.com").ShouldBe("a***@example.com");
        EmailMasking.Mask("a.very.long.name@example.com").ShouldBe("a***@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-at-sign")]
    [InlineData("@example.com")]
    public void Mask_HidesEverything_WhenThereIsNoNamePart(string email)
    {
        EmailMasking.Mask(email).ShouldBe("***");
    }

    #endregion
}
