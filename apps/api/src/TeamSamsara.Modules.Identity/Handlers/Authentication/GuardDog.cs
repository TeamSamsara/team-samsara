// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/GuardDog.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : Default IGuardDog. Fingerprints a client as a keyed hash of its IP address and
// user agent, so only the hash is ever stored.

using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Handlers;

public class GuardDog : IGuardDog
{
    #region Fields

    private const string UnknownValue = "unknown";
    private const string FingerprintSeparator = "|";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IClock _clock;
    private readonly GuardDogSettings _settings;

    #endregion

    #region Constructors

    public GuardDog(
        IHttpContextAccessor httpContextAccessor,
        IClock clock,
        IOptions<GuardDogSettings> settings)
    {
        _httpContextAccessor = httpContextAccessor;
        _clock = clock;
        _settings = settings.Value;
    }

    #endregion

    #region Public Methods

    // Describes the client making the current request
    public ClientInfo Inspect()
    {
        var context = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("There is no active HTTP request to inspect.");

        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? UnknownValue;
        var userAgent = context.Request.Headers.UserAgent.ToString();

        if (string.IsNullOrWhiteSpace(userAgent))
        {
            userAgent = UnknownValue;
        }

        return new ClientInfo(ipAddress, userAgent, ComputeFingerprint(ipAddress, userAgent));
    }

    // Whether the member has logged in from this client before
    public bool IsKnown(User user, ClientInfo client)
    {
        return user.KnownFingerprints.Any(known => known.Hash == client.Fingerprint);
    }

    // Records the client on the member, or refreshes it if already known
    public void Remember(User user, ClientInfo client)
    {
        var now = _clock.UtcNow;
        var existing = user.KnownFingerprints.FirstOrDefault(known => known.Hash == client.Fingerprint);

        if (existing is not null)
        {
            existing.LastSeenAt = now;
            return;
        }

        user.KnownFingerprints.Add(new KnownFingerprint
        {
            Hash = client.Fingerprint,
            LastSeenAt = now
        });

        TrimToLimit(user);
    }

    #endregion

    #region Private Methods

    // Keyed hash of the IP address and user agent, base64 encoded
    private string ComputeFingerprint(string ipAddress, string userAgent)
    {
        var key = Encoding.UTF8.GetBytes(_settings.Secret);
        var data = Encoding.UTF8.GetBytes(ipAddress + FingerprintSeparator + userAgent);

        return Convert.ToBase64String(HMACSHA256.HashData(key, data));
    }

    // Keeps only the most recently seen fingerprints
    private void TrimToLimit(User user)
    {
        user.KnownFingerprints = user.KnownFingerprints
            .OrderByDescending(known => known.LastSeenAt)
            .Take(_settings.MaxKnownFingerprints)
            .ToList();
    }

    #endregion
}
