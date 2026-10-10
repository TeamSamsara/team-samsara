// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/EmailChange/EmailMasking.cs
// Version : 1.0.0
// Latest commit: feat/email-change-service
// Author : Gerrah
// Purpose : Hides most of an email address so a notice can name it without exposing it in full.

namespace TeamSamsara.Modules.Identity.Handlers;

public static class EmailMasking
{
    #region Fields

    private const string Hidden = "***";

    #endregion

    #region Public Methods

    // Keeps the first character and the domain: "jane.doe@example.com" becomes "j***@example.com".
    public static string Mask(string email)
    {
        var at = email.LastIndexOf('@');

        if (at <= 0)
        {
            return Hidden;
        }

        return email[0] + Hidden + email[at..];
    }

    #endregion
}
