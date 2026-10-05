// File : /team-samsara/apps/web/src/features/account/view/AccountPlaceholder.tsx
// Version : 1.0.0
// Latest commit: feature/apps-web-foundation
// Author : Gerrah
// Purpose : Temporary placeholder for the authenticated account area,
// replaced by real account pages later. Split into its own file since a
// route-definition file exporting both a route array and a component
// breaks Fast Refresh (react-refresh/only-export-components).

export function AccountPlaceholder() {
  return <p>Account - coming soon</p>;
}
