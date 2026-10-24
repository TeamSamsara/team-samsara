// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/Authentication/AccountRecovery.cs
// Version : 1.0.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : The rules for bringing a deleted account back: it can be restored for a limited time
// after deletion, and never while a purge holds it.

using Microsoft.Extensions.Options;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Handlers;

public class AccountRecovery
{
    #region Fields

    private readonly IClock _clock;
    private readonly AuthenticationSettings _settings;

    #endregion

    #region Constructors

    public AccountRecovery(IClock clock, IOptions<AuthenticationSettings> settings)
    {
        _clock = clock;
        _settings = settings.Value;
    }

    #endregion

    #region Public Methods

    // A deleted account can only be restored for a limited time; after that it awaits purging
    public bool IsPastWindow(User user)
    {
        if (user.DeletedAt is not { } deletedAt)
        {
            return false;
        }

        return deletedAt.AddDays(_settings.RecoveryWindowDays) < _clock.UtcNow;
    }

    // Whether a purge currently holds the account, which makes it impossible to restore
    public bool IsBeingPurged(User user)
    {
        return user.PurgeLeaseUntil is { } heldUntil && heldUntil > _clock.UtcNow;
    }

    // Clears the deletion markers unless the window has ended or a purge holds the account
    public bool TryRestore(User user)
    {
        if (IsPastWindow(user) || IsBeingPurged(user))
        {
            return false;
        }

        user.DeletedAt = null;
        user.PurgeLeaseUntil = null;

        return true;
    }

    #endregion
}
