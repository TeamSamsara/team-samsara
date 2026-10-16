// File : /team-samsara/apps/web/src/shared/constants/branding.ts
// Version : 1.1.0
// Latest commit: task/web-auth-screens
// Author : Gerrah
// Purpose : The Samsara brand images, stored through the Assets module and referenced by
// asset id. Every screen that shows the brand reads its id from here; show an image with
// assetUrl(id).

export const brandAssetIds = {
  /** The mark alone (SVG). */
  icon: "0e9d0a9f-100b-4fb8-80e2-e4c96ffcdcee",
  /** The "Samsara" lettering alone (PNG, 460 x 75). */
  wordmark: "b643fd86-691d-4070-be3f-6adc60e79892",
  /** The mark with the lettering (PNG, 744 x 140). */
  logoFull: "5a2c17e7-94da-4356-bc44-9b965a40b7b5",
} as const;
