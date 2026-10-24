// File : /team-samsara/apps/web/src/shared/components/Modal/Modal.tsx
// Version : 1.0.0
// Latest commit: task/web-account-security
// Author : Gerrah
// Purpose : The generic modal: a titled card over a dimmed page, built on the browser's
// own dialog so focus stays inside and the page behind cannot be used. It opens when it
// appears and closes when it is removed; what it says and which buttons it has is up to
// the caller.

import {
  useEffect,
  useId,
  useRef,
  type MouseEvent,
  type ReactNode,
} from "react";
import styles from "./Modal.module.css";

interface ModalProps {
  readonly title: string;
  /** What the modal says and does: text, fields and buttons. */
  readonly children: ReactNode;
  /** Called when the person presses Esc or clicks the dimmed page, if dismissible. */
  readonly onClose?: () => void;
  /** False while something is in progress, so Esc and outside clicks do nothing. */
  readonly dismissible?: boolean;
  readonly className?: string;
}

export function Modal({
  title,
  children,
  onClose,
  dismissible = true,
  className,
}: ModalProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const titleId = useId();

  // Opening as a modal makes the browser trap focus and dim everything else.
  useEffect(() => {
    const dialog = dialogRef.current;

    if (!dialog) {
      return;
    }

    if (!dialog.open) {
      dialog.showModal();
    }

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    return () => {
      document.body.style.overflow = previousOverflow;
      if (dialog.open) {
        dialog.close();
      }
    };
  }, []);

  // Esc is handled here so the caller decides whether the modal may close.
  function handleCancel(event: Event | { preventDefault: () => void }) {
    event.preventDefault();

    if (dismissible) {
      onClose?.();
    }
  }

  // A click outside the card lands on the dialog itself; one inside is within its box.
  function handleClick(event: MouseEvent<HTMLDialogElement>) {
    if (!dismissible) {
      return;
    }

    const box = event.currentTarget.getBoundingClientRect();
    const isOutside =
      event.clientX < box.left ||
      event.clientX > box.right ||
      event.clientY < box.top ||
      event.clientY > box.bottom;

    if (isOutside) {
      onClose?.();
    }
  }

  return (
    <dialog
      ref={dialogRef}
      className={[styles.dialog, className].filter(Boolean).join(" ")}
      aria-labelledby={titleId}
      onCancel={handleCancel}
      onClick={handleClick}
    >
      <div className={styles.content}>
        <h2 id={titleId} className={styles.title}>
          {title}
        </h2>
        {children}
      </div>
    </dialog>
  );
}
