// File : /team-samsara/apps/web/src/features/account/view/memberProfileContent.ts
// Version : 1.0.0
// Latest commit: task/web-member-profile
// Author : Gerrah
// Purpose : The text of a member's profile page: the edit button, the empty bio
// hint for the owner, the socials placeholder, and what to say when the profile
// cannot be loaded.

export const MemberProfileText = {
  editProfile: "Edit profile",
  handlePrefix: "@",
  emptyBio: "You haven't written a bio yet. Use Edit profile to add one.",
  socialsTitle: "Socials",
  socialsSoon: "Linked accounts will show here soon.",
  pictureAlt: (displayName: string) => `${displayName}'s profile picture`,
  loadFailed: "We couldn't load this profile. Please try again.",
  rateLimited: "You're looking up profiles too quickly. Try again in a moment.",
  retry: "TRY AGAIN",
} as const;
