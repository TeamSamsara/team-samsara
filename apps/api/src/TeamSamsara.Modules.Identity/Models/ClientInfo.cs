// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/ClientInfo.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : What GuardDog learned about the client making a request: the raw values (for
// alert emails) and the fingerprint (for recognition).

namespace TeamSamsara.Modules.Identity.Models;

public record ClientInfo(string IpAddress, string UserAgent, string Fingerprint);
