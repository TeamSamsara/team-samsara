// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/BlockedWordsTests.cs
// Version : 1.0.0
// Latest commit: task/api-profile-onboarding
// Author : Gerrah
// Purpose : Proves the blocked-word filter: it catches disguised words in names and handles, spares innocent text, and has its word lists loaded.

using Shouldly;
using TeamSamsara.Modules.Identity.Handlers;

namespace TeamSamsara.Modules.Identity.Tests;

public class BlockedWordsTests
{
    #region Public Methods

    [Fact]
    public void TheWordListsAreLoaded()
    {
        // A missing embedded resource would turn the filter off silently, so this guards it
        BlockedWords.IsBlockedName("fuck").ShouldBeTrue();
        BlockedWords.IsBlockedName("merde").ShouldBeTrue();
    }

    [Theory]
    [InlineData("fuck")]
    [InlineData("FUCK")]
    [InlineData("fuuuck")]
    [InlineData("Hello fuck")]
    [InlineData("MERDE")]
    public void ABlockedWordInAName_IsCaught(string name)
    {
        BlockedWords.IsBlockedName(name).ShouldBeTrue();
    }

    [Theory]
    [InlineData("Gerrah")]
    [InlineData("Samsara")]
    [InlineData("classic")]
    [InlineData("Jean-Michel")]
    [InlineData("Ñoño")]
    public void AnInnocentName_IsAllowed(string name)
    {
        BlockedWords.IsBlockedName(name).ShouldBeFalse();
    }

    [Theory]
    [InlineData("fuck")]
    [InlineData("xfuckx")]
    [InlineData("f_u_c_k")]
    [InlineData("sh1t_head")]
    public void ABlockedWordInsideAHandle_IsCaught(string handle)
    {
        BlockedWords.IsBlockedHandle(handle).ShouldBeTrue();
    }

    [Theory]
    [InlineData("gerrah")]
    [InlineData("classic")]
    [InlineData("samsara_fan")]
    [InlineData("player_one")]
    public void AnInnocentHandle_IsAllowed(string handle)
    {
        BlockedWords.IsBlockedHandle(handle).ShouldBeFalse();
    }

    [Fact]
    public void AShortBlockedWord_OnlyBlocksAHandleWhenItIsTheWholeHandle()
    {
        // "ass" is on the list, but "classic" must stay available
        BlockedWords.IsBlockedHandle("ass").ShouldBeTrue();
        BlockedWords.IsBlockedHandle("classic").ShouldBeFalse();
    }

    #endregion
}
