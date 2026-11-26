// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Contact/Models/ContactFile.cs
// Version : 1.0.0
// Latest commit: feat/contact-module
// Author : Gerrah
// Purpose : One file a visitor attached to a contact form submission, as uploaded.

namespace TeamSamsara.Modules.Contact.Models;

public record ContactFile(string FileName, byte[] Content);
