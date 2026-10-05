// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Persistence/FirestoreServiceCollectionExtensions.cs
// Version : 1.0.2
// Latest commit: feature/api-host
// Author : Gerrah
// Purpose : Registers FirestoreDb as a singleton, using GoogleCredentialProvider for credentials.

using Google.Api.Gax;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TeamSamsara.Shared.Authentication;
using TeamSamsara.Shared.Configuration;

namespace TeamSamsara.Shared.Persistence;

public static class FirestoreServiceCollectionExtensions
{
    #region Public Methods

    public static IServiceCollection AddFirestore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddValidatedOptions<FirestoreSettings>(configuration, "Firestore");

        services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<FirestoreSettings>>().Value;

            return new FirestoreDbBuilder
            {
                ProjectId = settings.ProjectId,
                EmulatorDetection = EmulatorDetection.EmulatorOrProduction,
                GoogleCredential = GoogleCredentialProvider.TryGetFromEnvironment()
            }.Build();
        });

        return services;
    }

    #endregion
}
