// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/Fakes/InMemoryCacheService.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : An in-memory cache for tests. Entries never expire on their own; tests that care
// about expiry check what was stored and removed.

using TeamSamsara.Shared.Caching;

namespace TeamSamsara.Modules.Identity.Tests.Fakes;

public class InMemoryCacheService : ICacheService
{
    #region Fields

    private readonly Dictionary<string, object?> _entries = new();

    #endregion

    #region Properties

    // How many times something was stored
    public int SetCount { get; private set; }

    #endregion

    #region Public Methods

    public Task<T?> GetAsync<T>(string key)
    {
        var found = _entries.TryGetValue(key, out var value) && value is T typed;

        return Task.FromResult(found ? (T?)value : default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiry)
    {
        _entries[key] = value;
        SetCount++;

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _entries.Remove(key);

        return Task.CompletedTask;
    }

    #endregion
}
