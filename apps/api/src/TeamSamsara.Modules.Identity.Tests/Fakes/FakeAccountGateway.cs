// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/Fakes/FakeAccountGateway.cs
// Version : 1.2.0
// Latest commit: feat/email-change-foundation
// Author : Gerrah
// Purpose : A stand-in for the Firebase account operations that records what was applied.

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

    // The password last set on each account
    public Dictionary<string, string> Passwords { get; } = new();

    // Every account whose sessions were revoked, in order
    public List<string> RevokedSessions { get; } = new();

    // When true, setting the access level throws
    public bool SetAccessLevelShouldFail { get; set; }

    // When true, setting a password throws
    public bool SetPasswordShouldFail { get; set; }

    // When true, setting an email throws
    public bool SetEmailShouldFail { get; set; }

    #endregion

    #region Public Methods

    // Test helper: registers an account with this email
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

    public Task SetEmailAsync(string uid, string newEmail)
    {
        if (SetEmailShouldFail)
        {
            throw new InvalidOperationException("Simulated failure setting the email.");
        }

        if (!_emails.ContainsKey(uid))
        {
            throw new InvalidOperationException("The account does not exist.");
        }

        var holder = _emails
            .Where(entry => string.Equals(entry.Value, newEmail, StringComparison.OrdinalIgnoreCase))
            .Select(entry => entry.Key)
            .FirstOrDefault();

        if (holder is not null && holder != uid)
        {
            throw new InvalidOperationException("The email belongs to another account.");
        }

        _emails[uid] = newEmail;

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
