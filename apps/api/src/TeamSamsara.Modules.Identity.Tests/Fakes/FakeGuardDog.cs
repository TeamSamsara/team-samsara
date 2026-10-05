// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/Fakes/FakeGuardDog.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : A GuardDog whose current client tests choose, so a request "from" a known or
// unknown device can be simulated without an HTTP request. (The real GuardDog has its own
// tests.)

using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Tests.Fakes;

public class FakeGuardDog : IGuardDog
{
    #region Properties

    // The client the "current request" comes from
    public ClientInfo Current { get; set; } = new("203.0.113.7", "TestBrowser", "fingerprint-a");

    #endregion

    #region Public Methods

    public ClientInfo Inspect()
    {
        return Current;
    }

    public bool IsKnown(User user, ClientInfo client)
    {
        return user.KnownFingerprints.Any(fingerprint => fingerprint.Hash == client.Fingerprint);
    }

    public void Remember(User user, ClientInfo client)
    {
        var existing = user.KnownFingerprints.FirstOrDefault(fingerprint => fingerprint.Hash == client.Fingerprint);

        if (existing is not null)
        {
            return;
        }

        user.KnownFingerprints.Add(new KnownFingerprint
        {
            Hash = client.Fingerprint,
            LastSeenAt = DateTimeOffset.UtcNow
        });
    }

    #endregion
}
