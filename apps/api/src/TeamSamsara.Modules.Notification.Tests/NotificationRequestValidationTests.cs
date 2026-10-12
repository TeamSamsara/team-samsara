// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification.Tests/NotificationRequestValidationTests.cs
// Version : 1.0.0
// Latest commit: feat/notification-send-path
// Author : Gerrah
// Purpose : Proves the validator accepts the contract's boundaries and rejects each malformed recipient, key, dedupe key and data map without echoing the offending value.

using Shouldly;
using TeamSamsara.Modules.Notification.Handlers;
using TeamSamsara.Shared.Notification;

namespace TeamSamsara.Modules.Notification.Tests;

public class NotificationRequestValidationTests
{
    #region Fields

    private const string Member = "uid-member-1";
    private const string Key = "order.shipped";
    private const int MaxRecipientLength = 128;
    private const int MaxDedupeKeyLength = 100;
    private const int MaxDataEntries = 20;
    private const int MaxDataValueLength = 500;
    private const string FourTokenKey = "order.shipping.label.created";
    private const string FiveTokenKey = "order.shipping.label.created.again";

    #endregion

    #region Public Methods

    [Fact]
    public void Validate_WithAValidRequest_DoesNotThrow()
    {
        var request = new NotificationRequest(Member, Key, new Dictionary<string, string> { ["orderNumber"] = "1001" }, "order:1001:shipped");

        Should.NotThrow(() => NotificationRequestValidator.Validate(request));
    }

    [Fact]
    public void Validate_WithTheLargestAllowedValues_DoesNotThrow()
    {
        var data = Enumerable.Range(0, MaxDataEntries).ToDictionary(index => $"field{index}", _ => new string('x', MaxDataValueLength));
        var request = new NotificationRequest(
            new string('a', MaxRecipientLength),
            FourTokenKey,
            data,
            new string('d', MaxDedupeKeyLength));

        Should.NotThrow(() => NotificationRequestValidator.Validate(request));
    }

    [Fact]
    public void Validate_WithANullRequest_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => NotificationRequestValidator.Validate(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("has space")]
    [InlineData("a/b")]
    [InlineData("uid\n")]
    public void Validate_WithABadRecipient_Throws(string recipient)
    {
        var request = new NotificationRequest(recipient, Key);

        Should.Throw<ArgumentException>(() => NotificationRequestValidator.Validate(request));
    }

    [Fact]
    public void Validate_WithATooLongRecipient_Throws()
    {
        var request = new NotificationRequest(new string('a', MaxRecipientLength + 1), Key);

        Should.Throw<ArgumentException>(() => NotificationRequestValidator.Validate(request));
    }

    [Theory]
    [InlineData("")]
    [InlineData("welcome")]
    [InlineData("Order.shipped")]
    [InlineData("order..shipped")]
    [InlineData("order.")]
    [InlineData("1order.shipped")]
    [InlineData("order.shipped\n")]
    [InlineData(FiveTokenKey)]
    public void Validate_WithABadKey_Throws(string key)
    {
        var request = new NotificationRequest(Member, key);

        Should.Throw<ArgumentException>(() => NotificationRequestValidator.Validate(request));
    }

    [Theory]
    [InlineData("")]
    [InlineData("has space")]
    [InlineData("key\n")]
    public void Validate_WithABadDedupeKey_Throws(string dedupeKey)
    {
        var request = new NotificationRequest(Member, Key, null, dedupeKey);

        Should.Throw<ArgumentException>(() => NotificationRequestValidator.Validate(request));
    }

    [Fact]
    public void Validate_WithATooLongDedupeKey_Throws()
    {
        var request = new NotificationRequest(Member, Key, null, new string('d', MaxDedupeKeyLength + 1));

        Should.Throw<ArgumentException>(() => NotificationRequestValidator.Validate(request));
    }

    [Fact]
    public void Validate_WithTooManyDataEntries_Throws()
    {
        var data = Enumerable.Range(0, MaxDataEntries + 1).ToDictionary(index => $"field{index}", _ => "x");
        var request = new NotificationRequest(Member, Key, data);

        Should.Throw<ArgumentException>(() => NotificationRequestValidator.Validate(request));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Upper")]
    [InlineData("1field")]
    [InlineData("has space")]
    [InlineData("with-dash")]
    public void Validate_WithABadDataName_Throws(string name)
    {
        var request = new NotificationRequest(Member, Key, new Dictionary<string, string> { [name] = "x" });

        Should.Throw<ArgumentException>(() => NotificationRequestValidator.Validate(request));
    }

    [Fact]
    public void Validate_WithATooLongDataValue_Throws()
    {
        var request = new NotificationRequest(
            Member,
            Key,
            new Dictionary<string, string> { ["note"] = new string('x', MaxDataValueLength + 1) });

        Should.Throw<ArgumentException>(() => NotificationRequestValidator.Validate(request));
    }

    [Fact]
    public void Validate_WithANullDataValue_Throws()
    {
        var request = new NotificationRequest(
            Member,
            Key,
            new Dictionary<string, string> { ["note"] = null! });

        Should.Throw<ArgumentException>(() => NotificationRequestValidator.Validate(request));
    }

    [Fact]
    public void Validate_NeverEchoesTheOffendingValue()
    {
        const string secret = "secret value!";
        var request = new NotificationRequest(secret, Key);

        var exception = Should.Throw<ArgumentException>(() => NotificationRequestValidator.Validate(request));

        exception.Message.ShouldNotContain(secret);
    }

    #endregion
}
