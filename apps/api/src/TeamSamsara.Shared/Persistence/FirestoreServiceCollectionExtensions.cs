// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Persistence/FirestoreServiceCollectionExtensions.cs
// Version : 1.0.0
// Latest commit: feature/shared-core-primitives
// Author : Gerrah

// Purpose : Registers and configures the shared Firestore services.

using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TeamSamsara.Shared.Configuration;

namespace TeamSamsara.Shared.Persistence;

public static class FirestoreServiceCollectionExtensions
{
    #region Public Methods

    // Registers validated Firestore configuration and the shared database client.
    public static IServiceCollection AddFirestore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddValidatedOptions<FirestoreSettings>(configuration, "Firestore");

        services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<FirestoreSettings>>().Value;
            return FirestoreDb.Create(settings.ProjectId);
        });

        return services;
    }

    #endregion
}
