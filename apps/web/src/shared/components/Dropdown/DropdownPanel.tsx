// File : /team-samsara/apps/web/src/shared/components/Dropdown/DropdownPanel.tsx
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : The floating box a dropdown opens. It decides where it sits next
// to the trigger (placement and offset) and whether it stays in the page while
// closed; what goes inside, and how it looks, is up to the page.

import type { ComponentPropsWithoutRef, CSSProperties } from "react";
import { useDropdownContext } from "./useDropdownContext";
import styles from "./Dropdown.module.css";

export type DropdownPlacement =
  "bottom-start" | "bottom-end" | "top-start" | "top-end";

interface DropdownPanelProps extends ComponentPropsWithoutRef<"div"> {
  /** Which side of the trigger the panel opens on, and which edge it lines up with. */
  placement?: DropdownPlacement;
  /** Gap between trigger and panel, in design pixels (scaled like all geometry). */
  offset?: number;
  /** Keep the panel in the page while closed (hidden), for example to animate it. */
  keepMounted?: boolean;
}

export function DropdownPanel({
  placement = "bottom-start",
  offset = 0,
  keepMounted = false,
  className,
  style,
  children,
  ...divProps
}: DropdownPanelProps) {
  const { setPanel, isOpen, panelId, triggerId } = useDropdownContext();

  if (!isOpen && !keepMounted) return null;

  const offsetStyle = {
    "--dropdown-offset": `calc(${offset} * var(--px))`,
    ...style,
  } as CSSProperties;

  return (
    <div
      {...divProps}
      ref={setPanel}
      className={className ? `${styles.panel} ${className}` : styles.panel}
      id={panelId}
      aria-labelledby={triggerId}
      hidden={!isOpen}
      data-open={isOpen}
      data-placement={placement}
      style={offsetStyle}
    >
      {children}
    </div>
  );
}
