// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Contact.Tests/ContactModuleTests.cs
// Version : 1.0.0
// Latest commit: feat/contact-module
// Author : Gerrah
// Purpose : Proves the module's registrations resolve, its settings are bound and checked, and its form route is mapped.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using TeamSamsara.Modules.Contact.Handlers;
using TeamSamsara.Modules.Contact.Models;
using TeamSamsara.Modules.Contact.Tests.Fakes;
using TeamSamsara.Shared.Email;

namespace TeamSamsara.Modules.Contact.Tests;

public class ContactModuleTests
{
    #region Fields

    private const string SettingsPrefix = ContactSettings.SectionName + ":";
    private const string ValidAddress = "staff@example.com";
    private const string SecondAddress = "media@example.com";
    private const string NotAnAddress = "not-an-address";
    private const string ContactRoute = "/contact";

    private static readonly string[] _recipientKeys =
    {
        nameof(ContactSettings.SponsorshipRecipients),
        nameof(ContactSettings.GeneralInquiriesRecipients),
        nameof(ContactSettings.MediaRecipients),
        nameof(ContactSettings.CustomerServiceRecipients)
    };

    #endregion

    #region Public Methods

    [Fact]
    public void TheModule_ResolvesTheContactService()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>());
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IContactService>();

        service.ShouldBeOfType<ContactService>();
    }

    [Fact]
    public void TheSettings_AreBoundFromConfiguration()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            [SettingsPrefix + nameof(ContactSettings.MediaRecipients)] = $"{ValidAddress}, {SecondAddress}",
            [SettingsPrefix + nameof(ContactSettings.MaxFiles)] = "2"
        });

        var settings = provider.GetRequiredService<IOptions<ContactSettings>>().Value;

        settings.GetRecipients(ContactCategory.Media).ShouldBe([ValidAddress, SecondAddress]);
        settings.MaxFiles.ShouldBe(2);
    }

    [Fact]
    public void WithoutOverrides_TheDefaultLimitsApply()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>());

        var settings = provider.GetRequiredService<IOptions<ContactSettings>>().Value;

        settings.MaxFiles.ShouldBe(new ContactSettings().MaxFiles);
        settings.RateLimitPermitLimit.ShouldBe(new ContactSettings().RateLimitPermitLimit);
    }

    [Theory]
    [InlineData(nameof(ContactSettings.SponsorshipRecipients))]
    [InlineData(nameof(ContactSettings.GeneralInquiriesRecipients))]
    [InlineData(nameof(ContactSettings.MediaRecipients))]
    [InlineData(nameof(ContactSettings.CustomerServiceRecipients))]
    public void AMissingRecipientSetting_IsRejected(string key)
    {
        using var provider = BuildProvider(new Dictionary<string, string?> { [SettingsPrefix + key] = string.Empty });
        var options = provider.GetRequiredService<IOptions<ContactSettings>>();

        Should.Throw<OptionsValidationException>(() => options.Value);
    }

    [Fact]
    public void AnInvalidRecipientAddress_IsRejected()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            [SettingsPrefix + nameof(ContactSettings.SponsorshipRecipients)] = NotAnAddress
        });
        var options = provider.GetRequiredService<IOptions<ContactSettings>>();

        Should.Throw<OptionsValidationException>(() => options.Value);
    }

    [Fact]
    public void AFileLimitAboveTheTotalLimit_IsRejected()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            [SettingsPrefix + nameof(ContactSettings.MaxFileBytes)] = "2000",
            [SettingsPrefix + nameof(ContactSettings.MaxTotalBytes)] = "1000"
        });
        var options = provider.GetRequiredService<IOptions<ContactSettings>>();

        Should.Throw<OptionsValidationException>(() => options.Value);
    }

    [Fact]
    public void TheModule_MapsAPostRouteForTheForm()
    {
        var app = WebApplication.CreateBuilder().Build();

        new ContactModule().MapEndpoints(app);

        var endpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ToArray();

        var form = endpoints.ShouldHaveSingleItem();
        form.RoutePattern.RawText.ShouldNotBeNull().TrimEnd('/').ShouldBe(ContactRoute);
        form.Metadata.GetMetadata<HttpMethodMetadata>().ShouldNotBeNull().HttpMethods.ShouldBe(["POST"]);
    }

    #endregion

    #region Private Methods

    // Registers the module over valid recipient settings plus the given overrides, with a recording email sender
    private static ServiceProvider BuildProvider(Dictionary<string, string?> overrides)
    {
        var values = _recipientKeys.ToDictionary(key => SettingsPrefix + key, _ => (string?)ValidAddress);

        foreach (var (key, value) in overrides)
        {
            values[key] = value;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<IEmailSender, RecordingEmailSender>();
        new ContactModule().RegisterServices(services, configuration);

        return services.BuildServiceProvider();
    }

    #endregion
}
