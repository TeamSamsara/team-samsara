// File : /team-samsara/apps/web/src/features/contact/validation/contactValidationContent.ts
// Version : 1.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : The messages the contact form shows beside a field that cannot be sent as it is.

export const ContactValidationText = {
  firstNameRequired: "Enter your first name",
  lastNameRequired: "Enter your last name",
  nameTooLong: (max: number) => `Use at most ${max} characters`,
  phoneInvalid: "Use digits and + - ( ) . only",
  phoneTooLong: (max: number) => `Use at most ${max} characters`,
  categoryRequired: "Choose a subject",
  messageRequired: "Write your message",
  messageTooLong: (max: number) => `Use at most ${max} characters`,
  tooManyFiles: (max: number) => `Attach at most ${max} files`,
  fileTooLarge: (name: string, megabytes: number) =>
    `${name} is larger than ${megabytes} MB`,
  filesTooLarge: (megabytes: number) =>
    `The files together are larger than ${megabytes} MB`,
  fileTypeNotAccepted: (name: string) => `${name} is not an accepted file type`,
} as const;
