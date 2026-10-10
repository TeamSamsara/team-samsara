// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/Fakes/InMemoryEmailChangeRequestStore.cs
// Version : 1.0.0
// Latest commit: feat/email-change-request-store
// Author : Gerrah
// Purpose : In-memory email change request store; Take removes the record, like the real one.

using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Repositories;

namespace TeamSamsara.Modules.Identity.Tests.Fakes;

public class InMemoryEmailChangeRequestStore : IEmailChangeRequestStore
{
    #region Fields

    private readonly Dictionary<string, EmailChangeRequest> _requests = new();

    #endregion

    #region Properties

    // Number of pending requests currently stored.
    public int Count => _requests.Count;

    #endregion

    #region Public Methods

    public Task<EmailChangeRequest?> GetAsync(string userId)
    {
        return Task.FromResult(Find(userId));
    }

    public Task SaveAsync(EmailChangeRequest request)
    {
        _requests[request.Id] = Copy(request);

        return Task.CompletedTask;
    }

    // Sets the flag for the given purpose; does nothing if the request is gone.
    public Task MarkVerifiedAsync(string userId, VerificationPurpose purpose)
    {
        if (purpose is not (VerificationPurpose.EmailChangeOld or VerificationPurpose.EmailChangeNew))
        {
            throw new ArgumentOutOfRangeException(nameof(purpose), purpose, null);
        }

        if (!_requests.TryGetValue(userId, out var request))
        {
            return Task.CompletedTask;
        }

        if (purpose == VerificationPurpose.EmailChangeOld)
        {
            request.OldCodeVerified = true;
        }
        else
        {
            request.NewCodeVerified = true;
        }

        return Task.CompletedTask;
    }

    public Task<EmailChangeRequest?> TakeAsync(string userId)
    {
        if (!_requests.Remove(userId, out var request))
        {
            return Task.FromResult<EmailChangeRequest?>(null);
        }

        return Task.FromResult<EmailChangeRequest?>(request);
    }

    public Task DeleteAsync(string userId)
    {
        _requests.Remove(userId);

        return Task.CompletedTask;
    }

    // Test helper: reads a request without consuming it.
    public EmailChangeRequest? Find(string userId)
    {
        return _requests.TryGetValue(userId, out var request) ? Copy(request) : null;
    }

    #endregion

    #region Private Methods

    // Stores and returns copies, like a real database.
    private static EmailChangeRequest Copy(EmailChangeRequest request)
    {
        return new EmailChangeRequest
        {
            Id = request.Id,
            NewEmail = request.NewEmail,
            CreatedAt = request.CreatedAt,
            ExpiresAt = request.ExpiresAt,
            OldCodeVerified = request.OldCodeVerified,
            NewCodeVerified = request.NewCodeVerified
        };
    }

    #endregion
}
