// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Context/SystemClock.cs
// Version : 1.0.0
// Latest commit: feature/shared-core-primitives
// Author : Gerrah

// Purpose : Real implementation of IClock, backed by the actual system time.
// Registered as a singleton in DI; a test substitutes a fake IClock instead
// of using this class.

using System;

namespace TeamSamsara.Shared.Context;

public class SystemClock : IClock
{
   #region Properties
   public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
   #endregion
}
