// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Caching/MemoryCacheService.cs
// Version : 1.0.0
// Latest commit: feature/shared-core-primitives
// Author : Gerrah
// Purpose : Real implementation of ICacheService, backed by IMemoryCache.
// IMemoryCache itself is synchronous — the async signatures exist so a future
// Redis-backed implementation doesn't change any caller's code.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace TeamSamsara.Shared.Caching;

public class MemoryCacheService : ICacheService
{
    #region Fields

    private readonly IMemoryCache _memoryCache;

    #endregion

    #region Constructors

    public MemoryCacheService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    #endregion

    #region Public Methods

    public Task<T?> GetAsync<T>(string key)
    {
        _memoryCache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiry)
    {
        _memoryCache.Set(key, value, expiry);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _memoryCache.Remove(key);
        return Task.CompletedTask;
    }

    #endregion
}
