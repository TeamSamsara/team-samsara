// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Results/Error.cs
// Version : 1.0.0
// Latest commit: feature/shared-core-primitives
// Author : Gerrah

// Purpose : Provide Error, the value used by Result and Result<T> to describe
// a failure.

namespace TeamSamsara.Shared.Results;

public record Error(string Code, string Message, ErrorType Type)
{
    #region Fields

    // Represents the absence of an error; used by a successful Result.
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);

    #endregion

    #region Public Methods

    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);
    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);

    #endregion
}
