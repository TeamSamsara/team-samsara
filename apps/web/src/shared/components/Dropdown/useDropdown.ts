// File : /team-samsara/apps/web/src/shared/components/Dropdown/useDropdown.ts
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : The logic shared by every dropdown: open state, closing on Escape,
// outside click and focus leaving, and arrow-key movement between items. It
// renders nothing, so each page decides what its dropdown contains and looks like.

import {
  useCallback,
  useEffect,
  useId,
  useRef,
  useState,
  type FocusEvent,
  type KeyboardEvent,
} from "react";

/** Marker every selectable item carries, so the hook can find them in the panel. */
export const dropdownItemSelector = "[data-dropdown-item]:not([data-disabled])";

export type DropdownFocusTarget = "first" | "last" | "next" | "previous";

export interface UseDropdownOptions {
  /** Controlled open state. Leave out to let the dropdown manage it. */
  open?: boolean;
  /** Open state on first render when not controlled. */
  defaultOpen?: boolean;
  /** Called whenever the dropdown asks to open or close. */
  onOpenChange?: (open: boolean) => void;
}

export interface DropdownState {
  isOpen: boolean;
  triggerId: string;
  panelId: string;
  /** Pass as `ref` to the element that wraps the trigger and the panel. */
  setRoot: (element: HTMLDivElement | null) => void;
  /** Pass as `ref` to the element that opens the dropdown. */
  setTrigger: (element: HTMLElement | null) => void;
  /** Pass as `ref` to the element that holds the items. */
  setPanel: (element: HTMLElement | null) => void;
  open: () => void;
  /** Closes the dropdown. Focus goes back to the trigger unless told otherwise. */
  close: (options?: { restoreFocus?: boolean }) => void;
  toggle: () => void;
  focusItem: (target: DropdownFocusTarget) => void;
  /** Attach to the element that wraps the trigger and the panel. */
  handleKeyDown: (event: KeyboardEvent<HTMLElement>) => void;
  /** Attach to the same wrapper: closes when focus moves outside it. */
  handleBlur: (event: FocusEvent<HTMLElement>) => void;
}

export function useDropdown({
  open: controlledOpen,
  defaultOpen = false,
  onOpenChange,
}: UseDropdownOptions = {}): DropdownState {
  const baseId = useId();
  const rootElement = useRef<HTMLDivElement | null>(null);
  const triggerElement = useRef<HTMLElement | null>(null);
  const panelElement = useRef<HTMLElement | null>(null);
  const pendingFocus = useRef<DropdownFocusTarget | null>(null);

  const setRoot = useCallback((element: HTMLDivElement | null) => {
    rootElement.current = element;
  }, []);
  const setTrigger = useCallback((element: HTMLElement | null) => {
    triggerElement.current = element;
  }, []);
  const setPanel = useCallback((element: HTMLElement | null) => {
    panelElement.current = element;
  }, []);

  const [uncontrolledOpen, setUncontrolledOpen] = useState(defaultOpen);
  const isControlled = controlledOpen !== undefined;
  const isOpen = isControlled ? controlledOpen : uncontrolledOpen;

  const setOpen = useCallback(
    (next: boolean) => {
      if (!isControlled) setUncontrolledOpen(next);
      if (next !== isOpen) onOpenChange?.(next);
    },
    [isControlled, isOpen, onOpenChange],
  );

  const open = useCallback(() => setOpen(true), [setOpen]);

  const close = useCallback(
    ({ restoreFocus = true }: { restoreFocus?: boolean } = {}) => {
      setOpen(false);
      if (restoreFocus) triggerElement.current?.focus();
    },
    [setOpen],
  );

  const toggle = useCallback(() => setOpen(!isOpen), [setOpen, isOpen]);

  const focusItem = useCallback((target: DropdownFocusTarget) => {
    const items = Array.from(
      panelElement.current?.querySelectorAll<HTMLElement>(
        dropdownItemSelector,
      ) ?? [],
    );
    if (items.length === 0) return;

    const current = items.indexOf(document.activeElement as HTMLElement);
    let next = 0;
    if (target === "last") next = items.length - 1;
    if (target === "next") {
      next = current < 0 ? 0 : (current + 1) % items.length;
    }
    if (target === "previous") {
      next = current <= 0 ? items.length - 1 : current - 1;
    }
    items[next].focus();
  }, []);

  // Opening from the keyboard has to wait until the panel exists before it can
  // move focus into it.
  useEffect(() => {
    if (!isOpen || pendingFocus.current === null) return;
    focusItem(pendingFocus.current);
    pendingFocus.current = null;
  }, [isOpen, focusItem]);

  useEffect(() => {
    if (!isOpen) return;
    function handlePointerDown(event: PointerEvent) {
      if (!rootElement.current?.contains(event.target as Node)) setOpen(false);
    }
    document.addEventListener("pointerdown", handlePointerDown);
    return () => document.removeEventListener("pointerdown", handlePointerDown);
  }, [isOpen, setOpen]);

  const handleKeyDown = useCallback(
    (event: KeyboardEvent<HTMLElement>) => {
      const onTrigger = triggerElement.current?.contains(event.target as Node);

      if (event.key === "Escape" && isOpen) {
        event.preventDefault();
        close();
        return;
      }

      if (onTrigger && (event.key === "ArrowDown" || event.key === "ArrowUp")) {
        event.preventDefault();
        const target = event.key === "ArrowDown" ? "first" : "last";
        if (isOpen) {
          focusItem(target);
        } else {
          pendingFocus.current = target;
          open();
        }
        return;
      }

      if (!isOpen || onTrigger) return;

      const moves: Record<string, DropdownFocusTarget> = {
        ArrowDown: "next",
        ArrowUp: "previous",
        Home: "first",
        End: "last",
      };
      const move = moves[event.key];
      if (move) {
        event.preventDefault();
        focusItem(move);
      }
    },
    [isOpen, close, open, focusItem],
  );

  const handleBlur = useCallback(
    (event: FocusEvent<HTMLElement>) => {
      const next = event.relatedTarget as Node | null;
      if (isOpen && next && !event.currentTarget.contains(next)) {
        close({ restoreFocus: false });
      }
    },
    [isOpen, close],
  );

  return {
    isOpen,
    triggerId: `${baseId}-trigger`,
    panelId: `${baseId}-panel`,
    setRoot,
    setTrigger,
    setPanel,
    open,
    close,
    toggle,
    focusItem,
    handleKeyDown,
    handleBlur,
  };
}
