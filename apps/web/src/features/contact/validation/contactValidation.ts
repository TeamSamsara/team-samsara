// File : /team-samsara/apps/web/src/features/contact/validation/contactValidation.ts
// Version : 1.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : The rules the contact form checks before calling the API. Each returns the
// message to show, or null when the value is fine. The API repeats every check; these only
// save a trip.

import { validateEmail } from "@/features/auth/validation/authValidation";
import {
  contactFileExtensions,
  contactLimits,
  contactPhonePattern,
} from "../constants/contactConstants";
import type { ContactErrors, ContactFormValues } from "../types/contactTypes";
import { ContactValidationText } from "./contactValidationContent";

const bytesPerMegabyte = 1024 * 1024;

export function validateFirstName(value: string): string | null {
  return validateName(value, ContactValidationText.firstNameRequired);
}

export function validateLastName(value: string): string | null {
  return validateName(value, ContactValidationText.lastNameRequired);
}

function validateName(value: string, requiredMessage: string): string | null {
  const name = value.trim();

  if (name.length === 0) {
    return requiredMessage;
  }

  if (name.length > contactLimits.maxNameLength) {
    return ContactValidationText.nameTooLong(contactLimits.maxNameLength);
  }

  return null;
}

/** The phone number is optional. */
export function validatePhone(value: string): string | null {
  const phone = value.trim();

  if (phone.length === 0) {
    return null;
  }

  if (phone.length > contactLimits.maxPhoneLength) {
    return ContactValidationText.phoneTooLong(contactLimits.maxPhoneLength);
  }

  return contactPhonePattern.test(phone)
    ? null
    : ContactValidationText.phoneInvalid;
}

export function validateCategory(value: string): string | null {
  return value ? null : ContactValidationText.categoryRequired;
}

export function validateMessage(value: string): string | null {
  const message = value.trim();

  if (message.length === 0) {
    return ContactValidationText.messageRequired;
  }

  return message.length > contactLimits.maxMessageLength
    ? ContactValidationText.messageTooLong(contactLimits.maxMessageLength)
    : null;
}

function hasAcceptedExtension(name: string): boolean {
  const lower = name.toLowerCase();
  return contactFileExtensions.some((extension) => lower.endsWith(extension));
}

/** The first problem with the attached files, or null when they can all be sent. */
export function validateFiles(files: readonly File[]): string | null {
  if (files.length > contactLimits.maxFiles) {
    return ContactValidationText.tooManyFiles(contactLimits.maxFiles);
  }

  for (const file of files) {
    if (!hasAcceptedExtension(file.name)) {
      return ContactValidationText.fileTypeNotAccepted(file.name);
    }

    if (file.size > contactLimits.maxFileBytes) {
      return ContactValidationText.fileTooLarge(
        file.name,
        contactLimits.maxFileBytes / bytesPerMegabyte,
      );
    }
  }

  const total = files.reduce((sum, file) => sum + file.size, 0);

  return total > contactLimits.maxTotalBytes
    ? ContactValidationText.filesTooLarge(
        contactLimits.maxTotalBytes / bytesPerMegabyte,
      )
    : null;
}

/** Every problem in the form, keyed by field; empty when the form can be sent. */
export function validateContactForm(values: ContactFormValues): ContactErrors {
  const checks: ContactErrors = {
    firstName: validateFirstName(values.firstName) ?? undefined,
    lastName: validateLastName(values.lastName) ?? undefined,
    email: validateEmail(values.email) ?? undefined,
    phone: validatePhone(values.phone) ?? undefined,
    category: validateCategory(values.category) ?? undefined,
    message: validateMessage(values.message) ?? undefined,
    files: validateFiles(values.files) ?? undefined,
  };

  return Object.fromEntries(
    Object.entries(checks).filter(([, message]) => message !== undefined),
  ) as ContactErrors;
}
