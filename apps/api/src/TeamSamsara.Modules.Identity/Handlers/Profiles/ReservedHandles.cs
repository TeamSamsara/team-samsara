// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/Profiles/ReservedHandles.cs
// Version : 1.0.1
// Latest commit: task/api-naming-fix
// Author : Gerrah
// Purpose : The handles members may not take, because they name the team, its
// staff or the site's own pages.

namespace TeamSamsara.Modules.Identity.Handlers;

public static class ReservedHandles
{
    #region Fields

    private static readonly HashSet<string> _words = new(StringComparer.Ordinal)
    {
        "admin", "administrator", "moderator", "mod", "staff", "support", "help", "security",
        "official", "team", "teams", "samsara", "teamsamsara", "root", "system", "api", "www",
        "mail", "email", "null", "undefined", "anonymous", "me", "member", "members",
        "profile", "profiles", "account", "settings", "login", "logout", "register", "signup",
        "signin", "welcome", "home", "about", "contact", "shop", "store", "orders", "careers",
        "brand", "news", "legal", "terms", "privacy"
    };

    #endregion

    #region Public Methods

    // True if the lowercase handle is on the reserved list
    public static bool IsReserved(string handle)
    {
        return _words.Contains(handle);
    }

    #endregion
}
