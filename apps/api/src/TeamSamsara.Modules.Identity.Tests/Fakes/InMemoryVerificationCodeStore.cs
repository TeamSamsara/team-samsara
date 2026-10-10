// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/Fakes/InMemoryVerificationCodeStore.cs
// Version : 1.1.0
// Latest commit: fix/verification-code-single-use
// Author : Gerrah
// Purpose : An in-memory verification code store, keyed like the real one (one code per member
// and purpose).

using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Repositories;

namespace TeamSamsara.Modules.Identity.Tests.Fakes;

public class InMemoryVerificationCodeStore : IVerificationCodeStore
{
    #region Fields

    private readonly object _lock = new();
    private readonly Dictionary<string, VerificationCode> _codes = new();

    #endregion

    #region Public Methods

    public Task<VerificationCode?> GetAsync(string userId, VerificationPurpose purpose)
    {
        var id = VerificationCode.BuildId(userId, purpose);

        lock (_lock)
        {
            return Task.FromResult(_codes.TryGetValue(id, out var code) ? Copy(code) : null);
        }
    }

    public Task SaveAsync(VerificationCode code)
    {
        lock (_lock)
        {
            _codes[code.Id] = Copy(code);
        }

        return Task.CompletedTask;
    }

    public Task<int?> IncrementFailedAttemptsAsync(string userId, VerificationPurpose purpose)
    {
        var id = VerificationCode.BuildId(userId, purpose);

        lock (_lock)
        {
            if (!_codes.TryGetValue(id, out var code))
            {
                return Task.FromResult<int?>(null);
            }

            code.FailedAttempts++;

            return Task.FromResult<int?>(code.FailedAttempts);
        }
    }

    public Task<bool> TryTakeAsync(string userId, VerificationPurpose purpose, string codeHash)
    {
        var id = VerificationCode.BuildId(userId, purpose);

        lock (_lock)
        {
            if (!_codes.TryGetValue(id, out var code) || code.CodeHash != codeHash)
            {
                return Task.FromResult(false);
            }

            _codes.Remove(id);

            return Task.FromResult(true);
        }
    }

    public Task DeleteAsync(string userId, VerificationPurpose purpose)
    {
        lock (_lock)
        {
            _codes.Remove(VerificationCode.BuildId(userId, purpose));
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Private Methods

    private static VerificationCode Copy(VerificationCode code)
    {
        return new VerificationCode
        {
            Id = code.Id,
            UserId = code.UserId,
            Purpose = code.Purpose,
            CodeHash = code.CodeHash,
            Salt = code.Salt,
            CreatedAt = code.CreatedAt,
            ExpiresAt = code.ExpiresAt,
            FailedAttempts = code.FailedAttempts
        };
    }

    #endregion
}
