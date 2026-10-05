// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/IGuardDog.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : Recognizes clients. Inspects the current request, tells whether a member has been
// seen from that client before, and remembers new clients.

using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Handlers;

public interface IGuardDog
{
    #region Public Methods

    // Describes the client making the current request
    public ClientInfo Inspect();

    // Whether the member has logged in from this client before
    public bool IsKnown(User user, ClientInfo client);

    // Records the client on the member (or refreshes it if already known), dropping the least
    // recently seen beyond the configured limit. Changes the user in memory only - the caller
    // saves it.
    public void Remember(User user, ClientInfo client);

    #endregion
}
