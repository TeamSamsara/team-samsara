// File : /team-samsara/apps/web/src/shared/components/Dropdown/Dropdown.tsx
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : The root of every dropdown. Owns the state (through useDropdown),
// shares it with the dropdown parts, and handles the keyboard and focus rules
// on the element that wraps the trigger and the panel. It has no text, icons
// or routes, so each page assembles the parts and styles them.

import type { ReactNode } from "react";
import { DropdownContext } from "./DropdownContext";
import { useDropdown, type UseDropdownOptions } from "./useDropdown";
import styles from "./Dropdown.module.css";

interface DropdownProps extends UseDropdownOptions {
  className?: string;
  children: ReactNode;
}

export function Dropdown({ className, children, ...options }: DropdownProps) {
  const dropdown = useDropdown(options);
  // Destructured on purpose: passing `dropdown.setRoot` straight to `ref` makes
  // the react-hooks/refs lint treat the whole object as a ref.
  const { setRoot, isOpen, handleKeyDown, handleBlur } = dropdown;

  return (
    <DropdownContext.Provider value={dropdown}>
      <div
        ref={setRoot}
        className={className ? `${styles.root} ${className}` : styles.root}
        data-open={isOpen}
        onKeyDown={handleKeyDown}
        onBlur={handleBlur}
      >
        {children}
      </div>
    </DropdownContext.Provider>
  );
}
