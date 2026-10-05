// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Authentication/FirebaseAuthenticationHandler.cs
// Version : 1.2.0
// Latest commit: feature/identity-profile
// Author : Gerrah
// Purpose : Provides Firebase ID token authentication for incoming API requests. A token
// claiming Member (or Admin) is only honored as such when its sign-in has been verified
// (recognized client or confirmed challenge); otherwise the caller is treated as a Guest.
// Fails closed: if verification cannot be checked, the caller is a Guest.

using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamSamsara.Shared.Context;
using TeamSamsara.Shared.Http;

namespace TeamSamsara.Shared.Authentication;

public class FirebaseAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    #region Fields

    private readonly ICurrentUserContext _currentUserContext;
    private readonly IServiceProvider _services;

    #endregion

    #region Constructors

    // Initializes the handler with the current user context. The sign-in verifier is looked up
    // from the request's services only when a token needs it, so anonymous and Guest requests
    // never touch the verifier's storage.
    public FirebaseAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ICurrentUserContext currentUserContext,
        IServiceProvider services)
        : base(options, logger, encoder)
    {
        _currentUserContext = currentUserContext;
        _services = services;
    }

    #endregion

    #region Protected Methods

    // Verifies the Firebase ID token and populates the current user context.
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var idToken = ReadBearerToken();

        if (idToken is null)
        {
            return AuthenticateResult.NoResult();
        }

        try
        {
            var decodedToken =
                await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);

            var authTime = ReadAuthTime(decodedToken);
            var (accessLevel, authState) = await ResolveCallerAsync(decodedToken, authTime);

            PopulateContext(decodedToken.Uid, accessLevel, authState, authTime);

            return AuthenticateResult.Success(BuildTicket(decodedToken.Uid));
        }
        catch (Exception exception)
        {
            return AuthenticateResult.Fail(exception);
        }
    }

    #endregion

    #region Private Methods

    // Returns the bearer token from the Authorization header, or null if there is none.
    private string? ReadBearerToken()
    {
        var header = Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(header))
        {
            return null;
        }

        if (!header.StartsWith(HttpConstants.BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return header[HttpConstants.BearerPrefix.Length..].Trim();
    }

    // Returns when the token's sign-in happened (Unix seconds), or null if the token has no
    // readable auth_time. It stays the same across token refreshes, so it identifies a sign-in.
    private static long? ReadAuthTime(FirebaseToken decodedToken)
    {
        if (!decodedToken.Claims.TryGetValue(ClaimNames.AuthTime, out var rawAuthTime))
        {
            return null;
        }

        var text = Convert.ToString(rawAuthTime, CultureInfo.InvariantCulture);

        return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var authTime)
            ? authTime
            : null;
    }

    // The access level the caller may actually use and why: the claimed level, but only if
    // their sign-in is verified. No claim means registration was never confirmed; a claim with
    // an unverified sign-in means a verification step is still pending.
    private async Task<(AccessLevel AccessLevel, AuthState AuthState)> ResolveCallerAsync(
        FirebaseToken decodedToken,
        long? authTime)
    {
        var claimed = ReadClaimedAccessLevel(decodedToken);

        if (claimed == AccessLevel.Guest)
        {
            return (AccessLevel.Guest, AuthState.RegistrationIncomplete);
        }

        var verified = await IsSignInVerifiedAsync(decodedToken.Uid, authTime);

        return verified
            ? (claimed, AuthState.Member)
            : (AccessLevel.Guest, AuthState.VerificationRequired);
    }

    // The access level written on the token, or Guest if absent or unreadable.
    private static AccessLevel ReadClaimedAccessLevel(FirebaseToken decodedToken)
    {
        if (!decodedToken.Claims.TryGetValue(ClaimNames.AccessLevel, out var rawAccessLevel))
        {
            return AccessLevel.Guest;
        }

        return Enum.TryParse<AccessLevel>(rawAccessLevel?.ToString(), out var parsed)
            ? parsed
            : AccessLevel.Guest;
    }

    // Whether this sign-in has been verified. Any failure to find out counts as "no".
    private async Task<bool> IsSignInVerifiedAsync(string userId, long? authTime)
    {
        if (authTime is null)
        {
            return false;
        }

        try
        {
            var verifier = _services.GetRequiredService<ISignInVerifier>();

            return await verifier.IsVerifiedAsync(userId, authTime.Value);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Logger.LogWarning(exception, "Could not check the sign-in; treating the caller as a Guest.");

            return false;
        }
    }

    // Records who is calling on the scoped context the rest of the request reads from.
    private void PopulateContext(
        string userId,
        AccessLevel accessLevel,
        AuthState authState,
        long? authTime)
    {
        if (_currentUserContext is not CurrentUserContext mutableContext)
        {
            return;
        }

        mutableContext.IsAuthenticated = true;
        mutableContext.UserId = userId;
        mutableContext.AccessLevel = accessLevel;
        mutableContext.AuthState = authState;
        mutableContext.AuthTime = authTime;
    }

    // Builds the authentication ticket for the verified caller.
    private AuthenticationTicket BuildTicket(string userId)
    {
        var identity = new ClaimsIdentity(Scheme.Name);

        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));

        return new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
    }

    #endregion
}
