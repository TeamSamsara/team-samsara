// File : /team-samsara/apps/web/src/features/contact/service/contactService.ts
// Version : 1.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : Sends the contact form to the API as a multipart form. The answer becomes one
// result the page can act on; it never throws and carries no text.

import { apiFetchResult } from "@/lib/apiClient";
import {
  ContactApiRoutes,
  ContactFormFields,
} from "../constants/contactConstants";
import type {
  ContactFormValues,
  ContactRejection,
  ContactResult,
} from "../types/contactTypes";

interface ContactResponseBody {
  readonly status: string;
}

const HttpStatus = {
  tooManyRequests: 429,
} as const;

const rejectionsByStatus: Readonly<Record<string, ContactRejection>> = {
  invalidRequest: "invalidRequest",
  invalidField: "invalidField",
  tooManyFiles: "tooManyFiles",
  fileTooLarge: "fileTooLarge",
  unsupportedFileType: "unsupportedFileType",
};

function buildForm(values: ContactFormValues): FormData {
  const form = new FormData();

  form.append(ContactFormFields.firstName, values.firstName.trim());
  form.append(ContactFormFields.lastName, values.lastName.trim());
  form.append(ContactFormFields.email, values.email.trim());
  form.append(ContactFormFields.phone, values.phone.trim());
  form.append(ContactFormFields.category, values.category);
  form.append(ContactFormFields.message, values.message.trim());
  form.append(ContactFormFields.honeypot, values.honeypot);

  for (const file of values.files) {
    form.append(ContactFormFields.files, file, file.name);
  }

  return form;
}

function toResult(
  httpStatus: number,
  body: ContactResponseBody | null,
  retryAfterSeconds: number | null,
): ContactResult {
  if (httpStatus === HttpStatus.tooManyRequests) {
    return { kind: "rateLimited", retryAfterSeconds };
  }

  const status = body?.status;

  if (status === "sent") {
    return { kind: "sent" };
  }

  const reason = status ? rejectionsByStatus[status] : undefined;

  return reason ? { kind: "rejected", reason } : { kind: "failed" };
}

/** Sends one message to the team. */
export async function sendContactMessage(
  values: ContactFormValues,
): Promise<ContactResult> {
  try {
    const result = await apiFetchResult<ContactResponseBody>(
      ContactApiRoutes.submit,
      { method: "POST", body: buildForm(values) },
    );

    return toResult(result.httpStatus, result.body, result.retryAfterSeconds);
  } catch {
    // Only a network failure rejects.
    return { kind: "failed" };
  }
}
