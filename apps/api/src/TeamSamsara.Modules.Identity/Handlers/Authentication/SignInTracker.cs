// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/Authentication/SignInTracker.cs
// Version : 1.2.0
// Latest commit: feat/logout
// Author : Gerrah
// Purpose : Stores verified sign-ins on the user record and answers the per-request "is this sign-in verified?" question from a short-lived cache.

using Microsoft.Extensions.Options;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Repositories;
using TeamSamsara.Shared.Authentication;
using TeamSamsara.Shared.Caching;

namespace TeamSamsara.Modules.Identity.Handlers;

public class SignInTracker : ISignInTracker, ISignInVerifier
{
    #region Fields

    private const string CacheKeyPrefix = "identity:verified-sign-ins:";

    private readonly IUserStore _userStore;
    private readonly ICacheService _cache;
    private readonly AuthenticationSettings _settings;

    #endregion

    #region Constructors

    // Initializes the tracker with the user store, the cache and its settings.
    public SignInTracker(
        IUserStore userStore,
        ICacheService cache,
        IOptions<AuthenticationSettings> settings)
    {
        _userStore = userStore;
        _cache = cache;
        _settings = settings.Value;
    }

    #endregion

    #region Public Methods

    // Adds the sign-in to the member's verified list, trims the oldest, and drops the cache.
    public async Task RecordVerifiedAsync(string userId, long authTime)
    {
        await _userStore.ModifyAsync(userId, user => AddSignIn(user, authTime));
        await _cache.RemoveAsync(CacheKey(userId));
    }

    // Removes one sign-in from the member's verified list and drops the cache; unknown members are ignored.
    public async Task RemoveVerifiedAsync(string userId, long authTime)
    {
        if (await _userStore.GetByIdAsync(userId) is null)
        {
            return;
        }

        await _userStore.ModifyAsync(userId, user => user.VerifiedSignIns.Remove(authTime));
        await _cache.RemoveAsync(CacheKey(userId));
    }

    // Empties the member's verified list and drops the cache, so the change is seen at once.
    public async Task ClearVerifiedAsync(string userId)
    {
        await _userStore.ModifyAsync(userId, user => user.VerifiedSignIns.Clear());
        await _cache.RemoveAsync(CacheKey(userId));
    }

    // True only when the member exists, is not deleted, and this sign-in was verified.
    public async Task<bool> IsVerifiedAsync(string userId, long authTime)
    {
        var signIns = await GetVerifiedSignInsAsync(userId);

        return signIns.Contains(authTime);
    }

    #endregion

    #region Private Methods

    // Appends the sign-in once and keeps only the newest entries.
    private void AddSignIn(User user, long authTime)
    {
        if (!user.VerifiedSignIns.Contains(authTime))
        {
            user.VerifiedSignIns.Add(authTime);
        }

        var excess = user.VerifiedSignIns.Count - _settings.MaxVerifiedSignIns;

        if (excess > 0)
        {
            user.VerifiedSignIns.RemoveRange(0, excess);
        }
    }

    // Returns the cached list or loads it from the store; empty when the user cannot sign in.
    private async Task<List<long>> GetVerifiedSignInsAsync(string userId)
    {
        var cached = await _cache.GetAsync<List<long>>(CacheKey(userId));

        if (cached is not null)
        {
            return cached;
        }

        var user = await _userStore.GetByIdAsync(userId);

        if (user is null || user.DeletedAt is not null)
        {
            return new List<long>();
        }

        await _cache.SetAsync(
            CacheKey(userId),
            user.VerifiedSignIns,
            TimeSpan.FromSeconds(_settings.VerifiedCacheSeconds));

        return user.VerifiedSignIns;
    }

    // Cache key for one member's verified sign-ins.
    private static string CacheKey(string userId) => CacheKeyPrefix + userId;

    #endregion
}
