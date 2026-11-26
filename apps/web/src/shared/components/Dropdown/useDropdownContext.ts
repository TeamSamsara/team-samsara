// File : /team-samsara/apps/web/src/shared/components/Dropdown/useDropdownContext.ts
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : Reads DropdownContext for the dropdown parts, and fails loudly if
// a part is used outside <Dropdown>.

import { useContext } from "react";
import { DevInvariants } from "@/lib/devInvariants";
import { DropdownContext } from "./DropdownContext";
import type { DropdownState } from "./useDropdown";

export function useDropdownContext(): DropdownState {
  const context = useContext(DropdownContext);
  if (!context) {
    throw new Error(DevInvariants.dropdownPartOutsideDropdown);
  }
  return context;
}
