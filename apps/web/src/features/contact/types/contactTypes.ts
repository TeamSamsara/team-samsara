// File : /team-samsara/apps/web/src/features/contact/types/contactTypes.ts
// Version : 1.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : The values of the contact form and the outcomes of sending it. An outcome
// carries no text; the page decides what to say.

import type { ContactCategoryValue } from "../constants/contactConstants";

export interface ContactFormValues {
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
  readonly phone: string;
  /** Empty until a subject is chosen. */
  readonly category: ContactCategoryValue | "";
  readonly message: string;
  readonly files: readonly File[];
  /** Hidden from people; a bot that fills every field fills this one too. */
  readonly honeypot: string;
}

/** What the API says was wrong with a submission. */
export type ContactRejection =
  | "invalidField"
  | "tooManyFiles"
  | "fileTooLarge"
  | "unsupportedFileType"
  | "invalidRequest";

export type ContactResult =
  | { readonly kind: "sent" }
  | { readonly kind: "rejected"; readonly reason: ContactRejection }
  | { readonly kind: "rateLimited"; readonly retryAfterSeconds: number | null }
  /** The API could not send the email, or could not be reached. */
  | { readonly kind: "failed" };

export type ContactFieldName =
  | "firstName"
  | "lastName"
  | "email"
  | "phone"
  | "category"
  | "message"
  | "files";

export type ContactErrors = Partial<Record<ContactFieldName, string>>;
