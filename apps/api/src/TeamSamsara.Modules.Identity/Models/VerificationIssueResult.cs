// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/VerificationIssueResult.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : What the caller gets back after requesting a code: whether it was sent, how long
// the code is (so the client can draw the right input), and how long to wait if throttled.

namespace TeamSamsara.Modules.Identity.Models;

public record VerificationIssueResult(
    VerificationIssueStatus Status,
    int CodeLength,
    int RetryAfterSeconds);
