// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Http/IApiKeySettings.cs
// Version : 1.0.0
// Latest commit: feature/assets-module
// Author : Gerrah
// Purpose : Contract for a settings class exposing an API key

namespace TeamSamsara.Shared.Http;

public interface IApiKeySettings
{
    #region Properties

    // The shared secret a caller must present to reach the protected endpoint(s)
    public string ApiKey { get; }

    #endregion
}
