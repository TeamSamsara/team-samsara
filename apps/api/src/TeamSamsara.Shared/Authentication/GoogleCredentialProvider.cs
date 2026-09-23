// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Authentication/GoogleCredentialProvider.cs
// Version : 1.0.0
// Latest commit: feature/api-host
// Author : Gerrah
// Purpose : Reads FIREBASE_SERVICE_ACCOUNT_JSON and builds a GoogleCredential from it.

using Google.Apis.Auth.OAuth2;

namespace TeamSamsara.Shared.Authentication;

public static class GoogleCredentialProvider
{
    #region Fields
    #endregion
    private const string EnvironmentVariableName = "FIREBASE_SERVICE_ACCOUNT_JSON";

    #region Public Methods

    public static GoogleCredential? TryGetFromEnvironment()
    {
        var json = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        return string.IsNullOrWhiteSpace(json) ? null : GoogleCredential.FromJson(json);
    }
    #endregion

}
