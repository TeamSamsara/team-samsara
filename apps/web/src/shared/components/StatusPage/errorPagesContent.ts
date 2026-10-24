// File : /team-samsara/apps/web/src/shared/components/StatusPage/errorPagesContent.ts
// Version : 1.0.0
// Latest commit: task/web-error-pages
// Author : Gerrah
// Purpose : The text of the 404, 403 and 500 pages.

export const errorPagesContent = {
  notFound: {
    code: "404",
    title: "Page not found",
    body: "The page you are looking for does not exist or has been moved.",
    home: "Back to home",
  },
  forbidden: {
    code: "403",
    title: "No access",
    body: "You do not have permission to see this page.",
    home: "Back to home",
  },
  serverError: {
    code: "500",
    title: "Something went wrong",
    body: "We could not load this page. Check your connection and try again.",
    retry: "Try again",
    home: "Back to home",
  },
} as const;
