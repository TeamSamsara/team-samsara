// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/Fakes/InMemoryPasswordResetTokenStore.cs
// Version : 1.0.0
// Latest commit: feat/password-reset-service
// Author : Gerrah
// Purpose : In-memory password reset token store; Take removes the record, like the real one.

using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Repositories;

namespace TeamSamsara.Modules.Identity.Tests.Fakes;

public class InMemoryPasswordResetTokenStore : IPasswordResetTokenStore
{
    #region Fields

    private readonly Dictionary<string, PasswordResetToken> _tokens = new();

    #endregion

    #region Properties

    // Number of tokens currently stored.
    public int Count => _tokens.Count;

    #endregion

    #region Public Methods

    public Task SaveAsync(PasswordResetToken token)
    {
        _tokens[token.Id] = Copy(token);

        return Task.CompletedTask;
    }

    public Task<PasswordResetToken?> TakeAsync(string id)
    {
        if (!_tokens.Remove(id, out var token))
        {
            return Task.FromResult<PasswordResetToken?>(null);
        }

        return Task.FromResult<PasswordResetToken?>(token);
    }

    // Test helper: reads a record without consuming it.
    public PasswordResetToken? Find(string id)
    {
        return _tokens.TryGetValue(id, out var token) ? Copy(token) : null;
    }

    #endregion

    #region Private Methods

    // Stores and returns copies, like a real database.
    private static PasswordResetToken Copy(PasswordResetToken token)
    {
        return new PasswordResetToken
        {
            Id = token.Id,
            UserId = token.UserId,
            CreatedAt = token.CreatedAt,
            ExpiresAt = token.ExpiresAt
        };
    }

    #endregion
}
