// File : /team-samsara/apps/web/src/app/App.tsx
// Version : 1.0.0
// Latest commit: feature/apps-web-foundation
// Author : Gerrah
// Purpose : Composes every provider the app needs, in dependency order:
// QueryClientProvider (Layer 1) wraps AuthProvider (Layer 2) wraps
// ErrorBoundary (Layer 4) wraps the router (Layer 6). This is the final
// assembly point for everything built in items 6 through 11.

import { QueryClientProvider } from "@tanstack/react-query";
import { RouterProvider } from "react-router-dom";
import { queryClient } from "@/lib/queryClient";
import { AuthProvider } from "./AuthProvider";
import { ErrorBoundary } from "./ErrorBoundary";
import { router } from "./router";

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <ErrorBoundary>
          <RouterProvider router={router} />
        </ErrorBoundary>
      </AuthProvider>
    </QueryClientProvider>
  );
}
