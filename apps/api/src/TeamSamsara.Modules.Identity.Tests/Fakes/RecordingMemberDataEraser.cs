// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/Fakes/RecordingMemberDataEraser.cs
// Version : 1.0.0
// Latest commit: feat/notification-erasure
// Author : Gerrah
// Purpose : A member data eraser that records who it was asked to erase and can be made to fail, so the purge's use of erasers can be checked.

using TeamSamsara.Shared.Members;

namespace TeamSamsara.Modules.Identity.Tests.Fakes;

public class RecordingMemberDataEraser : IMemberDataEraser
{
    #region Properties

    public List<string> ErasedUserIds { get; } = new();

    public bool FailErase { get; set; }

    public Func<string, Task>? OnErase { get; set; }

    public CancellationToken LastCancellationToken { get; private set; }

    #endregion

    #region Public Methods

    public async Task EraseAsync(string userId, CancellationToken cancellationToken = default)
    {
        LastCancellationToken = cancellationToken;

        if (OnErase is not null)
        {
            await OnErase(userId);
        }

        if (FailErase)
        {
            throw new InvalidOperationException("The eraser was told to fail.");
        }

        ErasedUserIds.Add(userId);
    }

    #endregion
}
