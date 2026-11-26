// File : /team-samsara/apps/web/src/shared/components/Dropdown/DropdownItem.tsx
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : One choice inside a dropdown panel. It can be any element (a
// button by default, or a link, or your own component) and only adds what
// makes it an item: arrow-key navigation, a disabled state, a selected mark
// and closing the dropdown when chosen. What it says and looks like is up to
// the page.

import {
  useState,
  type ComponentPropsWithoutRef,
  type ElementType,
  type FocusEvent,
  type MouseEvent,
} from "react";
import { useDropdownContext } from "./useDropdownContext";

type DropdownItemProps<T extends ElementType> = {
  /** Element to render. Defaults to a button; pass Link or "a" for navigation. */
  as?: T;
  /** Marks the current choice (data-selected, plus aria for option and menu roles). */
  selected?: boolean;
  /** Skipped by arrow keys and ignores clicks. */
  disabled?: boolean;
  /** Close the dropdown after the item is chosen. */
  closeOnSelect?: boolean;
  /** Called when the item is chosen with the mouse, touch or keyboard. */
  onSelect?: () => void;
} & Omit<ComponentPropsWithoutRef<T>, "as" | "onSelect">;

export function DropdownItem<T extends ElementType = "button">({
  as,
  selected = false,
  disabled = false,
  closeOnSelect = true,
  onSelect,
  onClick,
  onFocus,
  onBlur,
  children,
  ...elementProps
}: DropdownItemProps<T>) {
  const { close } = useDropdownContext();
  const [isHighlighted, setIsHighlighted] = useState(false);
  const Element: ElementType = as ?? "button";
  const role = (elementProps as { role?: string }).role;

  function handleClick(event: MouseEvent<HTMLElement>) {
    if (disabled) {
      event.preventDefault();
      return;
    }
    onClick?.(event);
    if (event.defaultPrevented) return;
    onSelect?.();
    if (closeOnSelect) close();
  }

  return (
    <Element
      type={Element === "button" ? "button" : undefined}
      {...elementProps}
      data-dropdown-item=""
      data-selected={selected || undefined}
      data-disabled={disabled || undefined}
      data-highlighted={isHighlighted || undefined}
      aria-disabled={disabled || undefined}
      aria-selected={role === "option" ? selected : undefined}
      aria-checked={
        role === "menuitemradio" || role === "menuitemcheckbox"
          ? selected
          : undefined
      }
      // Items are reached with the arrow keys, so Tab goes past the dropdown.
      tabIndex={disabled ? -1 : (elementProps.tabIndex ?? -1)}
      onClick={handleClick}
      onFocus={(event: FocusEvent<HTMLElement>) => {
        setIsHighlighted(true);
        onFocus?.(event);
      }}
      onBlur={(event: FocusEvent<HTMLElement>) => {
        setIsHighlighted(false);
        onBlur?.(event);
      }}
    >
      {children}
    </Element>
  );
}
