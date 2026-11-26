// File : /team-samsara/apps/web/src/features/contact/constants/contactConstants.ts
// Version : 1.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : The contact form's API route, the form field names and the limits the API
// enforces. The values mirror ContactConstants and ContactSettings in the API (the file
// limits are its defaults); the API repeats every check, these only save a trip.

const bytesPerMebibyte = 1024 * 1024;

export const ContactApiRoutes = {
  submit: "/contact",
} as const;

/** Names of the multipart form fields the API reads. */
export const ContactFormFields = {
  firstName: "firstName",
  lastName: "lastName",
  email: "email",
  phone: "phone",
  category: "category",
  message: "message",
  files: "files",
  honeypot: "website",
} as const;

export const ContactCategoryValues = {
  generalInquiries: "general-inquiries",
  sponsorship: "sponsorship",
  media: "media",
  customerService: "customer-service",
} as const;

export type ContactCategoryValue =
  (typeof ContactCategoryValues)[keyof typeof ContactCategoryValues];

/** The order the subjects appear in the list. */
export const contactCategoryOrder: readonly ContactCategoryValue[] = [
  ContactCategoryValues.generalInquiries,
  ContactCategoryValues.sponsorship,
  ContactCategoryValues.media,
  ContactCategoryValues.customerService,
];

export const contactLimits = {
  maxNameLength: 100,
  maxPhoneLength: 30,
  maxMessageLength: 5000,
  maxFiles: 5,
  maxFileBytes: 5 * bytesPerMebibyte,
  maxTotalBytes: 8 * bytesPerMebibyte,
} as const;

/** The file types the API accepts as attachments. */
export const contactFileExtensions: readonly string[] = [
  ".png",
  ".jpg",
  ".jpeg",
  ".webp",
  ".pdf",
  ".docx",
  ".pptx",
  ".xlsx",
];

export const contactFileAccept = contactFileExtensions.join(",");

/** Digits and the symbols the API allows in a phone number. */
export const contactPhonePattern = /^[\d+\-(). ]*$/;
