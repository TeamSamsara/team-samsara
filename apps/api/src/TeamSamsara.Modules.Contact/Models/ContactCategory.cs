// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Contact/Models/ContactCategory.cs
// Version : 1.0.0
// Latest commit: feat/contact-module
// Author : Gerrah
// Purpose : The subject categories a visitor can pick on the contact form, each routed to its own recipients.

namespace TeamSamsara.Modules.Contact.Models;

public enum ContactCategory
{
    // Sponsorship and partnership requests
    Sponsorship,

    // Anything that fits no other category
    GeneralInquiries,

    // Press, creators and media requests
    Media,

    // Help with an order or an account
    CustomerService
}
