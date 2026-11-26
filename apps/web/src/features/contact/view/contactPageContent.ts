// File : /team-samsara/apps/web/src/features/contact/view/contactPageContent.ts
// Version : 1.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : The words of the contact page: the field labels, the subjects, and what the
// page says after a message was sent or could not be.

import {
  ContactCategoryValues,
  type ContactCategoryValue,
} from "../constants/contactConstants";
import type { ContactRejection } from "../types/contactTypes";

export const ContactPageText = {
  title: "CONTACT",
  firstName: "First name*",
  lastName: "Last name*",
  phone: "Phone number",
  email: "Email address*",
  subject: "Subject*",
  message: "Your message",
  attach: "Attach files",
  attachHint: (maxFiles: number, maxMegabytes: number) =>
    `Up to ${maxFiles} files, ${maxMegabytes} MB each. PNG, JPG, WEBP, PDF, DOCX, PPTX or XLSX.`,
  removeFile: (name: string) => `Remove ${name}`,
  send: "SEND",
  honeypot: "Leave this field empty",
} as const;

export const ContactCategoryLabels: Readonly<
  Record<ContactCategoryValue, string>
> = {
  [ContactCategoryValues.generalInquiries]: "General inquiries",
  [ContactCategoryValues.sponsorship]: "Sponsorship",
  [ContactCategoryValues.media]: "Media",
  [ContactCategoryValues.customerService]: "Customer service",
};

export const ContactResultText = {
  sentTitle: "Message sent",
  sentDetail: "Thank you for reaching out. We will get back to you soon.",
  failed:
    "We could not send your message. Check your connection and try again.",
  rateLimited: (retryAfterSeconds: number | null) => {
    if (retryAfterSeconds === null) {
      return "You have sent several messages. Please try again later.";
    }

    const minutes = Math.max(1, Math.ceil(retryAfterSeconds / 60));
    return `You have sent several messages. Please try again in ${minutes} minute${minutes === 1 ? "" : "s"}.`;
  },
  rejected: {
    invalidField:
      "Some of your details are not valid. Check them and try again.",
    invalidRequest: "Fill in every required field and try again.",
    tooManyFiles: "You attached too many files.",
    fileTooLarge: "One of your files is too large.",
    unsupportedFileType: "One of your files is not an accepted type.",
  } satisfies Record<ContactRejection, string>,
} as const;
