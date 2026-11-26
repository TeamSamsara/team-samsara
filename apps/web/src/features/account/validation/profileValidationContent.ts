// File : /team-samsara/apps/web/src/features/account/validation/profileValidationContent.ts
// Version : 1.1.0
// Latest commit: task/web-auth-onboarding-navbar
// Author : Gerrah
// Purpose : The messages shown under a profile field when its value is not
// acceptable.

export const ProfileValidationText = {
  displayNameRequired: "Enter the name people will see.",
  displayNameTooLong: (maxLength: number) =>
    `Use at most ${maxLength} characters.`,
  bioTooLong: (maxLength: number) => `Use at most ${maxLength} characters.`,
  handleRequired: "Choose a handle.",
  handleTooShort: (minLength: number) =>
    `Use at least ${minLength} characters.`,
  handleTooLong: (maxLength: number) => `Use at most ${maxLength} characters.`,
  handleCharacters: "Use only letters, numbers and underscores.",
  handleUnderscoreEdge: "A handle can't start or end with an underscore.",
  avatarType: "Use a JPEG, PNG or WebP image.",
  avatarTooBig: (megabytes: number) => `Use an image under ${megabytes} MB.`,
} as const;
