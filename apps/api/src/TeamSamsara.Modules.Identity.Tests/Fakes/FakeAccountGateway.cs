// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/Fakes/FakeAccountGateway.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : A stand-in for the Firebase account operations. Tests register accounts with an email
// and can read back the access level that was set.

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

    // When true, setting the access level throws (to test a failure partway through)
    public bool SetAccessLevelShouldFail { get; set; }

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

    public Task DeleteAsync(string uid)
    {
        _emails.Remove(uid);

        return Task.CompletedTask;
    }

    #endregion
}
