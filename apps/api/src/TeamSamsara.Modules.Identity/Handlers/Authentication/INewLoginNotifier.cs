// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/Authentication/INewLoginNotifier.cs
// Version : 1.0.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Tells a member that someone tried to sign in from a client they have not used before, by email and in their inbox.

using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Handlers;

public interface INewLoginNotifier
{
    // Never throws (except when cancelled): the sign-in must go on even when the member could not be told
    public Task NotifyAsync(
        string userId,
        string email,
        ClientInfo client,
        CancellationToken cancellationToken = default);
}
