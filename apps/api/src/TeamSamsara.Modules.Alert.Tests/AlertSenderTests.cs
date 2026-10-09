// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Alert.Tests/AlertSenderTests.cs
// Version : 1.1.0
// Latest commit: feat/alert-password-notices
// Author : Gerrah
// Purpose : Proves each alert type turns into one email to the right recipient with its template
// data filled in, that substituted values are HTML-encoded, and that an unknown type sends nothing.

using Shouldly;
using TeamSamsara.Modules.Alert.Tests.Fakes;
using TeamSamsara.Shared.Alert;

namespace TeamSamsara.Modules.Alert.Tests;

public class AlertSenderTests
{
    #region Fields

    private const string Recipient = "jane.doe@example.com";
    private const string VerificationCode = "493817";
    private const string Ip = "203.0.113.7";
    private const string Device = "Mozilla/5.0 (Test)";

    private readonly RecordingEmailSender _email = new();
    private readonly AlertSender _sender;

    #endregion

    #region Constructors

    public AlertSenderTests()
    {
        _sender = new AlertSender(_email);
    }

    #endregion

    #region Public Methods

    [Fact]
    public async Task RegistrationCode_SendsOneEmailToTheRecipientWithTheCode()
    {
        await _sender.SendAlertAsync(Recipient, AlertType.RegistrationCode, CodeData());

        var sent = _email.Sent.ShouldHaveSingleItem();
        sent.To.ShouldBe(Recipient);
        sent.Subject.ShouldNotBeNullOrWhiteSpace();
        sent.HtmlBody.ShouldContain(VerificationCode);
    }

    [Fact]
    public async Task StepUpCode_SendsOneEmailWithTheCode()
    {
        await _sender.SendAlertAsync(Recipient, AlertType.StepUpCode, CodeData());

        var sent = _email.Sent.ShouldHaveSingleItem();
        sent.To.ShouldBe(Recipient);
        sent.HtmlBody.ShouldContain(VerificationCode);
    }

    [Fact]
    public async Task NewLoginAttempt_SendsOneEmailWithTheIpAddressAndDevice()
    {
        await _sender.SendAlertAsync(Recipient, AlertType.NewLoginAttempt, ClientData());

        var sent = _email.Sent.ShouldHaveSingleItem();
        sent.To.ShouldBe(Recipient);
        sent.HtmlBody.ShouldContain(Ip);
        sent.HtmlBody.ShouldContain(Device);
    }

    [Fact]
    public async Task PasswordResetRequested_SendsOneEmailWithTheIpAddressAndDevice()
    {
        await _sender.SendAlertAsync(Recipient, AlertType.PasswordResetRequested, ClientData());

        var sent = _email.Sent.ShouldHaveSingleItem();
        sent.To.ShouldBe(Recipient);
        sent.Subject.ShouldNotBeNullOrWhiteSpace();
        sent.HtmlBody.ShouldContain(Ip);
        sent.HtmlBody.ShouldContain(Device);
    }

    [Fact]
    public async Task PasswordChanged_SendsOneEmailWithoutAnyTemplateData()
    {
        await _sender.SendAlertAsync(
            Recipient, AlertType.PasswordChanged, new Dictionary<string, string>());

        var sent = _email.Sent.ShouldHaveSingleItem();
        sent.To.ShouldBe(Recipient);
        sent.Subject.ShouldNotBeNullOrWhiteSpace();
        sent.HtmlBody.ShouldNotContain("{{");
    }

    [Fact]
    public async Task EveryAlertType_HasATemplateAndSendsExactlyOneEmail()
    {
        foreach (var type in Enum.GetValues<AlertType>())
        {
            await _sender.SendAlertAsync(Recipient, type, AllData());
        }

        _email.Sent.Count.ShouldBe(Enum.GetValues<AlertType>().Length);
        _email.Sent.ShouldAllBe(sent => sent.Subject.Length > 0 && sent.HtmlBody.Length > 0);
    }

    [Fact]
    public async Task EveryAlertType_LeavesNoPlaceholderUnfilled()
    {
        foreach (var type in Enum.GetValues<AlertType>())
        {
            await _sender.SendAlertAsync(Recipient, type, AllData());
        }

        _email.Sent.ShouldAllBe(sent => !sent.HtmlBody.Contains("{{"));
    }

    [Fact]
    public async Task SubstitutedValues_AreHtmlEncoded()
    {
        var data = new Dictionary<string, string>
        {
            [AlertTemplateKeys.IpAddress] = Ip,
            [AlertTemplateKeys.UserAgent] = "<script>alert(1)</script>"
        };

        await _sender.SendAlertAsync(Recipient, AlertType.NewLoginAttempt, data);

        var body = _email.Sent.ShouldHaveSingleItem().HtmlBody;
        body.ShouldNotContain("<script>");
        body.ShouldContain("&lt;script&gt;");
    }

    [Fact]
    public async Task ExtraTemplateData_IsIgnored()
    {
        var data = CodeData();
        data["unused"] = "should-not-appear";

        await _sender.SendAlertAsync(Recipient, AlertType.RegistrationCode, data);

        _email.Sent.ShouldHaveSingleItem().HtmlBody.ShouldNotContain("should-not-appear");
    }

    [Fact]
    public async Task CancellationToken_IsPassedToTheEmailSender()
    {
        using var source = new CancellationTokenSource();

        await _sender.SendAlertAsync(
            Recipient, AlertType.RegistrationCode, CodeData(), source.Token);

        _email.Sent.ShouldHaveSingleItem().CancellationToken.ShouldBe(source.Token);
    }

    [Fact]
    public async Task UnknownAlertType_Throws_AndSendsNothing()
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
            await _sender.SendAlertAsync(Recipient, (AlertType)999, CodeData()));

        _email.Sent.ShouldBeEmpty();
    }

    #endregion

    #region Private Methods

    private static Dictionary<string, string> CodeData() =>
        new() { [AlertTemplateKeys.Code] = VerificationCode };

    private static Dictionary<string, string> ClientData() =>
        new()
        {
            [AlertTemplateKeys.IpAddress] = Ip,
            [AlertTemplateKeys.UserAgent] = Device
        };

    private static Dictionary<string, string> AllData() =>
        new()
        {
            [AlertTemplateKeys.Code] = VerificationCode,
            [AlertTemplateKeys.IpAddress] = Ip,
            [AlertTemplateKeys.UserAgent] = Device
        };

    #endregion
}
