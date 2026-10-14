// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Contact/ContactModule.cs
// Version : 1.0.0
// Latest commit: feat/contact-module
// Author : Gerrah
// Purpose : Registers the Contact module's services and rate limit policy and maps its public form endpoint.

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using TeamSamsara.Modules.Contact.Endpoints;
using TeamSamsara.Modules.Contact.Handlers;
using TeamSamsara.Modules.Contact.Models;
using TeamSamsara.Shared.Configuration;
using TeamSamsara.Shared.Context;
using TeamSamsara.Shared.Http;
using TeamSamsara.Shared.Modules;

namespace TeamSamsara.Modules.Contact;

public class ContactModule : IModule
{
    #region Public Methods

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatedOptions<ContactSettings>(configuration, ContactSettings.SectionName);
        services.AddSingleton<IValidateOptions<ContactSettings>, ContactSettingsValidator>();

        services.TryAddSingleton<IClock, SystemClock>();

        services.AddSingleton<ContactSubmissionValidator>();
        services.AddSingleton<ContactAttachmentChecker>();
        services.AddSingleton<ContactEmailBuilder>();
        services.AddScoped<IContactService, ContactService>();

        var settings = configuration.GetSection(ContactSettings.SectionName).Get<ContactSettings>() ?? new ContactSettings();
        services.AddPerIpRateLimitPolicy(
            ContactConstants.RateLimitPolicyName,
            settings.RateLimitPermitLimit,
            settings.RateLimitWindowSeconds);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ContactEndpoints.Map(endpoints);
    }

    #endregion
}
