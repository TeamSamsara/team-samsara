// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Authentication/FirebaseAuthenticationHandler.cs
// Version : 1.0.0
// Latest commit: feature/api-host
// Author : Gerrah

// Purpose : Provides Firebase ID token authentication for incoming API requests.

using System.Security.Claims;
using System.Text.Encodings.Web;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Shared.Authentication;

public class FirebaseAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    #region Fields

    private readonly ICurrentUserContext _currentUserContext;

    #endregion

    #region Constructors

    // Initializes the handler with the current user context.
    public FirebaseAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ICurrentUserContext currentUserContext)
        : base(options, logger, encoder)
    {
        _currentUserContext = currentUserContext;
    }

    #endregion

    #region Protected Methods

    // Verifies the Firebase ID token and populates the current user context.
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(header)
            || !header.StartsWith(
                "Bearer ",
                StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var idToken = header["Bearer ".Length..].Trim();

        try
        {
            var decodedToken =
                await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);

            var accessLevel = AccessLevel.Guest;

            if (decodedToken.Claims.TryGetValue(
                    "accessLevel",
                    out var rawAccessLevel)
                && Enum.TryParse<AccessLevel>(
                    rawAccessLevel?.ToString(),
                    out var parsedAccessLevel))
            {
                accessLevel = parsedAccessLevel;
            }

            if (_currentUserContext is CurrentUserContext mutableContext)
            {
                mutableContext.IsAuthenticated = true;
                mutableContext.UserId = decodedToken.Uid;
                mutableContext.AccessLevel = accessLevel;
            }

            var identity = new ClaimsIdentity(Scheme.Name);

            identity.AddClaim(
                new Claim(
                    ClaimTypes.NameIdentifier,
                    decodedToken.Uid));

            var principal = new ClaimsPrincipal(identity);

            var ticket = new AuthenticationTicket(
                principal,
                Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception exception)
        {
            return AuthenticateResult.Fail(exception);
        }
    }

    #endregion
}
