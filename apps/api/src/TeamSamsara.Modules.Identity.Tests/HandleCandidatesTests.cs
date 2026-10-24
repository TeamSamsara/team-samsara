// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/HandleCandidatesTests.cs
// Version : 1.0.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Proves the handles offered to a member who has none: the display name first, numbered variants, the neutral fallback and the length limit.

using Shouldly;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Tests;

public class HandleCandidatesTests
{
    #region Fields

    private readonly ProfileSettings _settings = new();

    #endregion

    #region Public Methods

    [Fact]
    public void ADisplayName_IsTheFirstCandidate()
    {
        HandleCandidates.For("Gerrah", _settings).First().ShouldBe("gerrah");
    }

    [Fact]
    public void AccentsSpacesAndSymbols_AreDroppedFromTheFirstCandidate()
    {
        HandleCandidates.For("\u00c9lo\u00efse  Dupont!", _settings).First().ShouldBe("eloisedupont");
    }

    [Fact]
    public void AfterTheName_ComeNumberedVariants_ThenALastResort()
    {
        var candidates = HandleCandidates.For("Gerrah", _settings).ToList();

        candidates.Count.ShouldBe(10);
        candidates.Skip(1).Take(8).ShouldAllBe(candidate =>
            candidate.StartsWith("gerrah") && candidate.Length == "gerrah".Length + 4);
        candidates[^1].ShouldStartWith("member");
    }

    [Theory]
    [InlineData("Member")]
    [InlineData("ab")]
    [InlineData("!!!")]
    public void ANameThatCannotBeAHandle_FallsBackToTheNeutralHandle(string displayName)
    {
        var candidates = HandleCandidates.For(displayName, _settings).ToList();

        candidates.Count.ShouldBe(9);
        candidates.ShouldAllBe(candidate => candidate.StartsWith("member"));
        candidates.ShouldNotContain("member");
    }

    [Fact]
    public void ALongName_IsCutSoEveryCandidateFitsTheLimit()
    {
        var candidates = HandleCandidates.For(new string('a', 60), _settings).ToList();

        candidates.First().Length.ShouldBe(_settings.HandleMaxLength);
        candidates.ShouldAllBe(candidate => candidate.Length <= _settings.HandleMaxLength);
    }

    [Theory]
    [InlineData("Gerrah")]
    [InlineData("Member")]
    [InlineData("\u00c9lo\u00efse  Dupont!")]
    public void EveryCandidate_IsAValidHandle(string displayName)
    {
        HandleCandidates.For(displayName, _settings)
            .ShouldAllBe(candidate => HandleFormat.IsValid(candidate, _settings));
    }

    #endregion
}
