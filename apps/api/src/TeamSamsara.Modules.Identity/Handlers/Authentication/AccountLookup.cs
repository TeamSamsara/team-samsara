// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/Authentication/AccountLookup.cs
// Version : 1.0.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : The outcome of loading a member for sign-in: either an account allowed to sign in
// (User and Email set) or the reason it is not.

using System.Diagnostics.CodeAnalysis;
using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Handlers;

internal sealed class AccountLookup
{
    #region Properties

    public User? User { get; private init; }
    public string? Email { get; private init; }
    public AuthenticationStatus Rejection { get; private init; }

    [MemberNotNullWhen(false, nameof(User), nameof(Email))]
    public bool IsRejected { get; private init; }

    #endregion

    #region Public Methods

    public static AccountLookup Accept(User user, string email)
    {
        return new AccountLookup { User = user, Email = email };
    }

    public static AccountLookup Reject(AuthenticationStatus rejection)
    {
        return new AccountLookup { Rejection = rejection, IsRejected = true };
    }

    #endregion
}
