// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Http/FirestoreHealthCheck.cs
// Version : 1.0.1
// Latest commit: feature/string-magic-value-conventions
// Author : Gerrah
// Purpose : Confirms Firestore is reachable for the /health/ready endpoint.
// Reads from a dedicated "_health" collection, never used for real
// application data, so this check can never depend on or interfere with
// actual data existing.

using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TeamSamsara.Shared.Persistence;

namespace TeamSamsara.Shared.Http;

public class FirestoreHealthCheck : IHealthCheck
{
    #region Fields

    private readonly FirestoreDb _firestoreDb;

    #endregion

    #region Constructors

    public FirestoreHealthCheck(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
    }

    #endregion

    #region Public Methods

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _firestoreDb
                .Collection(FirestoreCollections.Health)
                .Limit(1)
                .GetSnapshotAsync(cancellationToken);

            return HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(HealthCheckConstants.FirestoreUnreachableMessage, exception);
        }
    }

    #endregion
}
