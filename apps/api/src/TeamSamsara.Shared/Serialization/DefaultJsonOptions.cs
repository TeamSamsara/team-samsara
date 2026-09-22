// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Serialization/DefaultJsonOptions.cs
// Version : 1.0.0
// Latest commit: feature/shared-core-primitives
// Author : Gerrah

// Purpose : Single shared JsonSerializerOptions instance used across the app,

using System.Text.Json;
using System.Text.Json.Serialization;

namespace TeamSamsara.Shared.Serialization;

public static class DefaultJsonOptions
{
    #region Fields

    public static readonly JsonSerializerOptions Instance = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    #endregion
}
