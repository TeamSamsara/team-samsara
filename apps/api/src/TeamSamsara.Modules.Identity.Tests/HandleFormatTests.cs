// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/HandleFormatTests.cs
// Version : 1.0.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Proves the handle format rules: normalizing, valid and invalid shapes, reserved handles and the refusal reasons.

using Shouldly;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Tests;

public class HandleFormatTests
{
    #region Fields

    private readonly ProfileSettings _settings = new();

    #endregion

    #region Public Methods

    [Theory]
    [InlineData("  @Samsara_Fan ", "samsara_fan")]
    [InlineData("GERRAH", "gerrah")]
    [InlineData("@@double", "double")]
    [InlineData(null, "")]
    public void Normalizing_TrimsLowercasesAndDropsTheAt(string? raw, string expected)
    {
        HandleFormat.Normalize(raw).ShouldBe(expected);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("a_b")]
    [InlineData("user_123")]
    [InlineData("abcdefghijklmnopqrst")]
    public void AWellFormedHandle_IsValid(string handle)
    {
        HandleFormat.IsValid(handle, _settings).ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("_abc")]
    [InlineData("abc_")]
    [InlineData("has space")]
    [InlineData("bad-char")]
    [InlineData("\u00e9mile")]
    [InlineData("abcdefghijklmnopqrstu")]
    public void AMalformedHandle_IsNotValid(string handle)
    {
        HandleFormat.IsValid(handle, _settings).ShouldBeFalse();
    }

    [Fact]
    public void TheLengthLimits_FollowTheSettings()
    {
        var settings = new ProfileSettings { HandleMinLength = 5, HandleMaxLength = 6 };

        HandleFormat.IsValid("abcd", settings).ShouldBeFalse();
        HandleFormat.IsValid("abcde", settings).ShouldBeTrue();
        HandleFormat.IsValid("abcdef", settings).ShouldBeTrue();
        HandleFormat.IsValid("abcdefg", settings).ShouldBeFalse();
    }

    [Fact]
    public void AReservedHandle_IsForbidden_AndAnOrdinaryOneIsNot()
    {
        HandleFormat.IsForbidden("admin").ShouldBeTrue();
        HandleFormat.IsForbidden("gerrah").ShouldBeFalse();
    }

    [Fact]
    public void TryNormalize_AcceptsAUsableHandle_AndReturnsItNormalized()
    {
        var usable = HandleFormat.TryNormalize(" @Gerrah ", _settings, out var handle, out var refusal);

        usable.ShouldBeTrue();
        handle.ShouldBe("gerrah");
        refusal.ShouldBe(ProfileStatus.Success);
    }

    [Fact]
    public void TryNormalize_RefusesAMalformedHandleAsInvalid()
    {
        var usable = HandleFormat.TryNormalize("no", _settings, out _, out var refusal);

        usable.ShouldBeFalse();
        refusal.ShouldBe(ProfileStatus.InvalidHandle);
    }

    [Fact]
    public void TryNormalize_RefusesAReservedHandleAsTaken()
    {
        var usable = HandleFormat.TryNormalize("Admin", _settings, out _, out var refusal);

        usable.ShouldBeFalse();
        refusal.ShouldBe(ProfileStatus.HandleTaken);
    }

    #endregion
}
