// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Email/EmailServiceCollectionExtensions.cs
// Version : 1.0.0
// Latest commit: feature/alerts-module
// Author : Gerrah
// Purpose : Registers the Resend-backed IEmailSender. Called directly from Program.cs, the same
// way AddAssetStorage and AddFirestore are - the email channel is cross-cutting infrastructure.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TeamSamsara.Shared.Configuration;
using TeamSamsara.Shared.Http;

namespace TeamSamsara.Shared.Email;

public static class EmailServiceCollectionExtensions
{
    #region Fields

    private const string ResendBaseUrl = "https://api.resend.com";

    #endregion

    #region Public Methods

    public static IServiceCollection AddEmail(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddValidatedOptions<EmailSettings>(configuration, EmailSettings.SectionName);

        services.AddHttpClient<IEmailSender, ResendEmailSender>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<EmailSettings>>().Value;

            client.BaseAddress = new Uri(ResendBaseUrl);
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "Authorization", HttpConstants.BearerPrefix + settings.ApiKey);
        });

        return services;
    }

    #endregion
}
