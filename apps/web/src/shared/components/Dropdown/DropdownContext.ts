// File : /team-samsara/apps/web/src/shared/components/Dropdown/DropdownContext.ts
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : The context that lets the dropdown parts (trigger, panel, items)
// read and control the state owned by <Dropdown>. Context only, like
// CarouselContext.ts; the hook that reads it is useDropdownContext.

import { createContext } from "react";
import type { DropdownState } from "./useDropdown";

export const DropdownContext = createContext<DropdownState | undefined>(
  undefined,
);
