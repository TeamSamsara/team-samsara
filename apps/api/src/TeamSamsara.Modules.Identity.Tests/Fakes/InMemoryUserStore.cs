// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/Fakes/InMemoryUserStore.cs
// Version : 1.1.0
// Latest commit: feat/account-purge
// Author : Gerrah
// Purpose : An in-memory user store. Stores and returns copies, like a real database, so a
// service that changes a user but forgets to save it is caught by the tests.

using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Repositories;

namespace TeamSamsara.Modules.Identity.Tests.Fakes;

public class InMemoryUserStore : IUserStore
{
    #region Fields

    private readonly Dictionary<string, User> _users = new();

    #endregion

    #region Public Methods

    public Task<User?> GetByIdAsync(string id)
    {
        return Task.FromResult(_users.TryGetValue(id, out var user) ? Copy(user) : null);
    }

    public Task CreateAsync(User user)
    {
        if (!_users.TryAdd(user.Id, Copy(user)))
        {
            throw new InvalidOperationException($"User '{user.Id}' already exists.");
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user)
    {
        _users[user.Id] = Copy(user);

        return Task.CompletedTask;
    }

    public Task ModifyAsync(string id, Action<User> modify)
    {
        if (!_users.TryGetValue(id, out var stored))
        {
            throw new InvalidOperationException($"User '{id}' does not exist.");
        }

        var copy = Copy(stored);
        modify(copy);
        _users[id] = copy;

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<User>> ListDeletedBeforeAsync(DateTimeOffset cutoff)
    {
        IReadOnlyList<User> result = _users.Values
            .Where(user => user.DeletedAt is not null && user.DeletedAt < cutoff)
            .Select(Copy)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<bool> TryClaimForPurgeAsync(
        string id,
        DateTimeOffset cutoff,
        DateTimeOffset now,
        DateTimeOffset leaseUntil)
    {
        if (!_users.TryGetValue(id, out var stored))
        {
            return Task.FromResult(false);
        }

        if (stored.DeletedAt is not { } deletedAt || deletedAt >= cutoff)
        {
            return Task.FromResult(false);
        }

        if (stored.PurgeLeaseUntil is { } heldUntil && heldUntil > now)
        {
            return Task.FromResult(false);
        }

        var copy = Copy(stored);
        copy.PurgeLeaseUntil = leaseUntil;
        _users[id] = copy;

        return Task.FromResult(true);
    }

    public Task DeleteAsync(string id)
    {
        _users.Remove(id);

        return Task.CompletedTask;
    }

    // Test helper: puts a user in the store
    public void Seed(User user)
    {
        _users[user.Id] = Copy(user);
    }

    #endregion

    #region Private Methods

    private static User Copy(User user)
    {
        return new User
        {
            Id = user.Id,
            AccessLevel = user.AccessLevel,
            CreatedAt = user.CreatedAt,
            DeletedAt = user.DeletedAt,
            PurgeLeaseUntil = user.PurgeLeaseUntil,
            KnownFingerprints = user.KnownFingerprints
                .Select(fingerprint => new KnownFingerprint
                {
                    Hash = fingerprint.Hash,
                    LastSeenAt = fingerprint.LastSeenAt
                })
                .ToList(),
            VerifiedSignIns = user.VerifiedSignIns.ToList()
        };
    }

    #endregion
}
