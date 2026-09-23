// File : /team-samsara/app/api/src/TeamSamsara.Shared/Results/Result.cs
// Version : 1.0.0
// Latest commit: feature/shared-core-primitives
// Author : Gerrah

// Purpose : Provide Result and Result<T>
// Represents operation outcomes without using exceptions for expected failures.

using System;

namespace TeamSamsara.Shared.Results;

public class Result
{
    #region Properties

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }
    #endregion

    #region Constructors

    // Enforces successful result never carries an error
    // Prevents inconsistent Result from being constructed
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("A successful result cannot have an error."); // To Do : replace string messages by shared constants
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("A failed result must contain an error."); // To Do : replace string messages by shared constants
        }

        IsSuccess = isSuccess;
        Error = error;
    }
    #endregion

    #region Public Methods

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    #endregion

}
