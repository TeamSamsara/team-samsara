// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Assets/Models/AssetFileResult.cs
// Version : 1.0.0
// Latest commit: feature/asset-storage-routing
// Author : Gerrah
// Purpose : Represents how an asset's file should be served - proxied directly as a
// stream, or redirected to a URL the client fetches on its own.

using System.IO;

namespace TeamSamsara.Modules.Assets.Models;

public class AssetFileResult
{
    #region Properties

    public Stream? Content { get; private init; }
    public string? ContentType { get; private init; }
    public string? RedirectUrl { get; private init; }

    #endregion

    #region Public Methods

    // Creates a result that should be proxied directly to the client as a stream
    public static AssetFileResult Proxied(Stream content, string contentType)
        => new() { Content = content, ContentType = contentType };

    // Creates a result that should redirect the client to fetch the file itself
    public static AssetFileResult Redirect(string redirectUrl)
        => new() { RedirectUrl = redirectUrl };

    #endregion
}
