// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Repositories/IVerificationCodeStore.cs
// Version : 1.1.0
// Latest commit: fix/verification-code-single-use
// Author : Gerrah
// Purpose : Looks up, saves, counts attempts against, and removes pending verification codes,
// independent of where they are stored.

using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Repositories;

public interface IVerificationCodeStore
{
    #region Public Methods

    // Retrieves the pending code for a member and purpose, or null if there is none
    public Task<VerificationCode?> GetAsync(string userId, VerificationPurpose purpose);

    // Saves a code, replacing any existing code for the same member and purpose
    public Task SaveAsync(VerificationCode code);

    // Atomically adds one to the code's failed-attempt count and returns the new count, or null
    // if no code exists. Callers increment BEFORE comparing a guess, so parallel guesses each
    // get a distinct count and the attempt limit cannot be outrun.
    public Task<int?> IncrementFailedAttemptsAsync(string userId, VerificationPurpose purpose);

    // Atomically deletes the code only if it still has the given hash, and answers true for the one
    // caller that deleted it. Used to consume a correct code, so parallel submissions of it cannot
    // all succeed and a code issued meanwhile is never consumed by a submission for the old one.
    public Task<bool> TryTakeAsync(string userId, VerificationPurpose purpose, string codeHash);

    // Deletes the pending code for a member and purpose
    public Task DeleteAsync(string userId, VerificationPurpose purpose);

    #endregion
}
