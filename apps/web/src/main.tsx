// File : /team-samsara/apps/web/src/main.tsx
// Version : 1.0.0
// Latest commit: feature/apps-web-foundation
// Author : Gerrah
// Purpose : The app's real entry point, referenced by index.html. Imports
// the one global stylesheet and mounts App into the DOM.

import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { App } from "./app/App";
import "@/shared/styling/global.css";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
