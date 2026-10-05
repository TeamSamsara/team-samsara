// File : /team-samsara/apps/web/src/lib/queryClient.ts
// Version : 1.0.0
// Latest commit: <branch name>
// Author : Gerrah
// Purpose : The single QueryClient instance for the app, provided once in
// main.tsx. Centralizing default options here means no feature has to
// configure its own retry/staleness behavior individually.

import { QueryClient } from "@tanstack/react-query";

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30 * 1000,
      retry: 1,
    },
  },
});
