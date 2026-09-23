// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Caching/ICacheService.cs
// Version : 1.0.0
// Latest commit: feature/shared-core-primitives
// Author : Gerrah
// Purpose : Abstracts caching so a future move to a distributed backend
// (Redis) only changes the implementation, not any caller. Expiry is always
// required — no cache entry is set without an explicit lifetime.

using System;
using System.Threading.Tasks;

namespace TeamSamsara.Shared.Caching;

public interface ICacheService
{
    #region Public Methods

    public Task<T?> GetAsync<T>(string key);
    public Task SetAsync<T>(string key, T value, TimeSpan expiry);
    public Task RemoveAsync(string key);

    #endregion
}
