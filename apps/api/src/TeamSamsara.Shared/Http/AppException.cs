// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Http/AppException.cs
// Version : 1.0.0
// Latest commit: feature/shared-core-primitives
// Author : Gerrah
// Purpose : Carries a domain error through the global exception-handling pipeline.
using TeamSamsara.Shared.Results;

namespace TeamSamsara.Shared.Http;

public class AppException : Exception
{
    #region Properties

    public Error Error { get; }

    #endregion

    #region Constructors

    public AppException(Error error)
        : base(error.Message)
    {
        Error = error;
    }

    #endregion
}
