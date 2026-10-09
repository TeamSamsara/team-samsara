// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/Fakes/FakeAccountGateway.cs
// Version : 1.1.0
// Latest commit: feat/account-gateway-password
// Author : Gerrah
// Purpose : A stand-in for the Firebase account operations. Tests register accounts with an email
// and can read back the access level, password and session revocations that were applied.

using TeamSamsara.Modules.Identity.Repositories;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Tests.Fakes;

public class FakeAccountGateway : IAccountGateway
{
    #region Fields

    private readonly Dictionary<string, string> _emails = new();

    #endregion

    #region Properties

    // The access level last set on each account
    public Dictionary<string, AccessLevel> AccessLevels { get; } = new();

    // The password last set on each account (only accounts whose password was changed)
    public Dictionary<string, string> Passwords { get; } = new();

    // Every account whose sessions were revoked, in order
    public List<string> RevokedSessions { get; } = new();

    // When true, setting the access level throws (to test a failure partway through)
    public bool SetAccessLevelShouldFail { get; set; }

    // When true, setting a password throws (to test a failure partway through)
    public bool SetPasswordShouldFail { get; set; }

    #endregion

    #region Public Methods

    // Test helper: an account that exists with this email
    public void AddAccount(string uid, string email)
    {
        _emails[uid] = email;
    }

    public Task SetAccessLevelAsync(string uid, AccessLevel accessLevel)
    {
        if (SetAccessLevelShouldFail)
        {
            throw new InvalidOperationException("Simulated failure setting the access level.");
        }

        AccessLevels[uid] = accessLevel;

        return Task.CompletedTask;
    }

    public Task<string?> GetEmailAsync(string uid)
    {
        return Task.FromResult(_emails.TryGetValue(uid, out var email) ? email : null);
    }

    public Task<string?> GetUserIdByEmailAsync(string email)
    {
        var uid = _emails
            .Where(entry => string.Equals(entry.Value, email, StringComparison.OrdinalIgnoreCase))
            .Select(entry => entry.Key)
            .FirstOrDefault();

        return Task.FromResult(uid);
    }

    public Task SetPasswordAsync(string uid, string newPassword)
    {
        if (SetPasswordShouldFail)
        {
            throw new InvalidOperationException("Simulated failure setting the password.");
        }

        if (!_emails.ContainsKey(uid))
        {
            throw new InvalidOperationException("The account does not exist.");
        }

        Passwords[uid] = newPassword;

        return Task.CompletedTask;
    }

    public Task RevokeSessionsAsync(string uid)
    {
        RevokedSessions.Add(uid);

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string uid)
    {
        _emails.Remove(uid);

        return Task.CompletedTask;
    }

    #endregion
}
