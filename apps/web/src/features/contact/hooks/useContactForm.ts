// File : /team-samsara/apps/web/src/features/contact/hooks/useContactForm.ts
// Version : 1.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : The state and rules of the contact form: what was typed, what is wrong with it,
// and what happened when it was sent. A message shows next to a field once the person leaves
// it or tries to send, and goes away as soon as the value is fixed.

import { useState, type FormEvent } from "react";
import { useToast } from "@/shared/components/Toast/useToast";
import { sendContactMessage } from "../service/contactService";
import type {
  ContactErrors,
  ContactFieldName,
  ContactFormValues,
  ContactResult,
} from "../types/contactTypes";
import {
  validateCategory,
  validateContactForm,
  validateFiles,
  validateFirstName,
  validateLastName,
  validateMessage,
  validatePhone,
} from "../validation/contactValidation";
import { validateEmail } from "@/features/auth/validation/authValidation";
import { ContactResultText } from "../view/contactPageContent";

const emptyForm: ContactFormValues = {
  firstName: "",
  lastName: "",
  email: "",
  phone: "",
  category: "",
  message: "",
  files: [],
  honeypot: "",
};

const fieldChecks: Record<
  ContactFieldName,
  (values: ContactFormValues) => string | null
> = {
  firstName: (values) => validateFirstName(values.firstName),
  lastName: (values) => validateLastName(values.lastName),
  email: (values) => validateEmail(values.email),
  phone: (values) => validatePhone(values.phone),
  category: (values) => validateCategory(values.category),
  message: (values) => validateMessage(values.message),
  files: (values) => validateFiles(values.files),
};

function setError(
  errors: ContactErrors,
  name: ContactFieldName,
  message: string | null,
): ContactErrors {
  const next = { ...errors };

  if (message) {
    next[name] = message;
  } else {
    delete next[name];
  }

  return next;
}

function sameFile(a: File, b: File): boolean {
  return a.name === b.name && a.size === b.size;
}

function failureText(result: Exclude<ContactResult, { kind: "sent" }>): string {
  switch (result.kind) {
    case "rejected":
      return ContactResultText.rejected[result.reason];
    case "rateLimited":
      return ContactResultText.rateLimited(result.retryAfterSeconds);
    case "failed":
      return ContactResultText.failed;
  }
}

export function useContactForm() {
  const toast = useToast();
  const [values, setValues] = useState<ContactFormValues>(emptyForm);
  const [errors, setErrors] = useState<ContactErrors>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  function changeField<Name extends keyof ContactFormValues>(
    name: Name,
    value: ContactFormValues[Name],
  ) {
    const next = { ...values, [name]: value };
    setValues(next);

    // An error that is showing goes away as soon as the value is fixed.
    if (name in fieldChecks && errors[name as ContactFieldName]) {
      const field = name as ContactFieldName;
      setErrors(setError(errors, field, fieldChecks[field](next)));
    }
  }

  function blurField(name: ContactFieldName) {
    setErrors(setError(errors, name, fieldChecks[name](values)));
  }

  function addFiles(chosen: readonly File[]) {
    const merged = [...values.files];

    for (const file of chosen) {
      if (!merged.some((existing) => sameFile(existing, file))) {
        merged.push(file);
      }
    }

    const next = { ...values, files: merged };
    setValues(next);
    setErrors(setError(errors, "files", validateFiles(merged)));
  }

  function removeFile(file: File) {
    const files = values.files.filter((existing) => existing !== file);
    setValues({ ...values, files });
    setErrors(setError(errors, "files", validateFiles(files)));
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (isSubmitting) {
      return;
    }

    const found = validateContactForm(values);
    setErrors(found);
    setFormError(null);

    if (Object.keys(found).length > 0) {
      return;
    }

    setIsSubmitting(true);
    const result = await sendContactMessage(values);
    setIsSubmitting(false);

    if (result.kind === "sent") {
      setValues(emptyForm);
      setErrors({});
      toast.show({
        tone: "success",
        title: ContactResultText.sentTitle,
        detail: ContactResultText.sentDetail,
      });
      return;
    }

    setFormError(failureText(result));
  }

  return {
    values,
    errors,
    formError,
    isSubmitting,
    changeField,
    blurField,
    addFiles,
    removeFile,
    submit,
  };
}
