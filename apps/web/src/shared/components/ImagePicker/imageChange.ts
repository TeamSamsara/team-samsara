// File : /team-samsara/apps/web/src/shared/components/ImagePicker/imageChange.ts
// Version : 1.0.0
// Latest commit: task/web-account-settings
// Author : Gerrah
// Purpose : What a member did with an image in a form: kept the saved one,
// chose a new file, or asked for it to be removed.

export type ImageChange =
  | { readonly kind: "keep" }
  | { readonly kind: "replace"; readonly file: File }
  | { readonly kind: "remove" };

export const keepImage: ImageChange = { kind: "keep" };
