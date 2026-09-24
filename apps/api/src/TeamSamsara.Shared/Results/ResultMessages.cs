// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Results/ResultMessages.cs
// Version : 1.0.0
// Latest commit: feature/string-magic-value-conventions
// Author : Gerrah
// Purpose : Messages used by Result/Result<T> and the global exception handler.

namespace TeamSamsara.Shared.Results;

public static class ResultMessages
{
    #region Fields

    public const string SuccessfulResultCannotHaveError = "A successful result cannot have an error.";
    public const string FailedResultMustContainError = "A failed result must contain an error.";
    public const string CannotAccessValueOfFailedResult = "Cannot access the value of a failed result.";
    public const string UnexpectedError = "An unexpected error occurred.";

    #endregion
}
