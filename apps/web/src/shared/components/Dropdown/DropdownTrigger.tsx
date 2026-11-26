// File : /team-samsara/apps/web/src/shared/components/Dropdown/DropdownTrigger.tsx
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : The button that opens and closes a dropdown. It carries the
// accessibility attributes that link it to the panel and nothing else, so any
// content (text, an icon, an avatar) and any look can go inside it.

import type { ComponentPropsWithoutRef, MouseEvent } from "react";
import { useDropdownContext } from "./useDropdownContext";

interface DropdownTriggerProps extends ComponentPropsWithoutRef<"button"> {
  /** What kind of popup the trigger opens, announced by screen readers. */
  popup?: "menu" | "listbox" | "dialog" | "true";
}

export function DropdownTrigger({
  popup = "true",
  onClick,
  children,
  ...buttonProps
}: DropdownTriggerProps) {
  const { setTrigger, toggle, isOpen, triggerId, panelId } =
    useDropdownContext();

  function handleClick(event: MouseEvent<HTMLButtonElement>) {
    onClick?.(event);
    if (!event.defaultPrevented) toggle();
  }

  return (
    <button
      {...buttonProps}
      ref={setTrigger}
      id={triggerId}
      type="button"
      aria-haspopup={popup}
      aria-expanded={isOpen}
      aria-controls={isOpen ? panelId : undefined}
      data-open={isOpen}
      onClick={handleClick}
    >
      {children}
    </button>
  );
}
