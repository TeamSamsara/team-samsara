// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Contact/Models/ContactStatus.cs
// Version : 1.0.0
// Latest commit: feat/contact-module
// Author : Gerrah
// Purpose : The outcome of one contact form submission.

namespace TeamSamsara.Modules.Contact.Models;

public enum ContactStatus
{
    // The email reached the recipients, or the submission was silently dropped as spam
    Sent,

    // The request was not a form, or it had no usable fields
    InvalidRequest,

    // A text field was missing, malformed or too long, or the category is unknown
    InvalidField,

    // More files were attached than the limit allows
    TooManyFiles,

    // One file, or all files together, exceeded the size limit
    FileTooLarge,

    // A file has a type we do not accept, or its content does not match its type
    UnsupportedFileType,

    // The email could not be sent, so nothing reached the team
    DeliveryFailed
}
