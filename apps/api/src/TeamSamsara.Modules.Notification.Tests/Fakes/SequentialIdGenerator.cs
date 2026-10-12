// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification.Tests/Fakes/SequentialIdGenerator.cs
// Version : 1.0.0
// Latest commit: feat/notification-send-path
// Author : Gerrah
// Purpose : An id generator that returns predictable ids in order, so tests can assert on generated document ids.

using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Notification.Tests.Fakes;

public class SequentialIdGenerator : IIdGenerator
{
    #region Fields

    private int _last;

    #endregion

    #region Public Methods

    public Guid NewId()
    {
        return new Guid(Interlocked.Increment(ref _last), 0, 0, new byte[8]);
    }

    #endregion
}
