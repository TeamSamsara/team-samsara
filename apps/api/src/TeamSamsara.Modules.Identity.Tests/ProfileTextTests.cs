// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/ProfileTextTests.cs
// Version : 1.0.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Proves the profile text rules: display name and bio limits, control characters, line breaks and how characters are counted.

using Shouldly;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Tests;

public class ProfileTextTests
{
    #region Fields

    private readonly ProfileSettings _settings = new();

    #endregion

    #region Public Methods

    [Theory]
    [InlineData("Gerrah")]
    [InlineData("\u00c9")]
    [InlineData("Jean-Fran\u00e7ois O'Brien")]
    public void AnOrdinaryDisplayName_IsValid(string name)
    {
        ProfileText.IsValidDisplayName(name, _settings).ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("bad\u0007name")]
    [InlineData("line\nbreak")]
    public void AnEmptyOrControlCharacterDisplayName_IsNotValid(string? name)
    {
        ProfileText.IsValidDisplayName(name, _settings).ShouldBeFalse();
    }

    [Fact]
    public void ADisplayName_MustFitTheLimit()
    {
        ProfileText.IsValidDisplayName(new string('a', 50), _settings).ShouldBeTrue();
        ProfileText.IsValidDisplayName(new string('a', 51), _settings).ShouldBeFalse();
    }

    [Fact]
    public void AnEmoji_CountsAsOneCharacter()
    {
        var fifty = string.Concat(Enumerable.Repeat("\U0001F600", 50));
        var fiftyOne = string.Concat(Enumerable.Repeat("\U0001F600", 51));

        ProfileText.IsValidDisplayName(fifty, _settings).ShouldBeTrue();
        ProfileText.IsValidDisplayName(fiftyOne, _settings).ShouldBeFalse();
    }

    [Fact]
    public void NoBio_IsValid()
    {
        ProfileText.IsValidBio(null, _settings).ShouldBeTrue();
    }

    [Fact]
    public void ABio_MustFitTheLimit()
    {
        ProfileText.IsValidBio(new string('a', 300), _settings).ShouldBeTrue();
        ProfileText.IsValidBio(new string('a', 301), _settings).ShouldBeFalse();
    }

    [Theory]
    [InlineData("two\nlines")]
    [InlineData("windows\r\nlines")]
    public void ABio_MayContainLineBreaks(string bio)
    {
        ProfileText.IsValidBio(bio, _settings).ShouldBeTrue();
    }

    [Theory]
    [InlineData("bell\u0007")]
    [InlineData("tab\there")]
    public void ABio_MayNotContainOtherControlCharacters(string bio)
    {
        ProfileText.IsValidBio(bio, _settings).ShouldBeFalse();
    }

    [Fact]
    public void TheLimits_FollowTheSettings()
    {
        var settings = new ProfileSettings { DisplayNameMaxLength = 5, BioMaxLength = 10 };

        ProfileText.IsValidDisplayName("abcdef", settings).ShouldBeFalse();
        ProfileText.IsValidBio(new string('a', 11), settings).ShouldBeFalse();
    }

    [Fact]
    public void AnAccentedLetter_AndAnEmoji_EachCountOnce()
    {
        ProfileText.LengthOf("e\u0301").ShouldBe(1);
        ProfileText.LengthOf("\U0001F600").ShouldBe(1);
        ProfileText.LengthOf("abc").ShouldBe(3);
    }

    #endregion
}
