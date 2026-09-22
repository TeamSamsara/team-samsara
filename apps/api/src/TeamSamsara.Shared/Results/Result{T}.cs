// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Results/Result{T}.cs
// Version : 1.0.0
// Latest commit: feature/shared-core-primitives
// Author : Gerrah

// Purpose : Provide Result<T>, extending Result to carry a value on success.

using System;

namespace TeamSamsara.Shared.Results;

public class Result<T> : Result
{
    #region Fields

    private readonly T? _value;

    #endregion

    #region Properties

    // Throws rather than returning default(T) on a failed result, so a caller
    // that forgets to check IsSuccess fails loudly instead of silently.
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result."); // To Do : replace string messages by shared constants

    #endregion

    #region Constructors

    private Result(T? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    #endregion

    #region Public Methods

    public static Result<T> Success(T value) => new(value, true, Error.None);
    public static new Result<T> Failure(Error error) => new(default, false, error);

    // Lets a method return a plain T and have it implicitly become a
    // successful Result<T>, avoiding Result<T>.Success(...) at every return site.
    public static implicit operator Result<T>(T value) => Success(value);

    #endregion
}
