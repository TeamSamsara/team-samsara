// File : /team-samsara/apps/web/src/shared/components/Fields/CodeInput/CodeInput.tsx
// Version : 1.1.0
// Latest commit: task/web-toast-messages
// Author : Gerrah
// Purpose : The generic one-time code input, one box per digit. It handles
// typing, deleting, arrow keys and pasting; what the code is for, and when it
// is sent, is decided by the caller.

import {
  useId,
  useRef,
  type ChangeEvent,
  type ClipboardEvent,
  type KeyboardEvent,
  type MouseEvent,
} from "react";
import { FieldError } from "../FieldError/FieldError";
import { CodeInputText } from "./codeInputContent";
import styles from "./CodeInput.module.css";

export interface CodeInputProps {
  /** How many digits the code has; one box is drawn for each. */
  length: number;
  /** The digits typed so far. Always filled from the left, with no gaps. */
  value: string;
  onChange: (value: string) => void;
  /** Called when the person's typing or pasting fills the last box. */
  onComplete?: (value: string) => void;
  /** Read by screen readers as the name of the whole group. */
  label: string;
  /** The message under the boxes. Leave empty when the code is fine. */
  error?: string | null;
  disabled?: boolean;
  /** Puts the cursor in the first box when the input appears. */
  autoFocus?: boolean;
  className?: string;
}

const NON_DIGITS = /\D/g;
const SINGLE_DIGIT = /^\d$/;

export function CodeInput({
  length,
  value,
  onChange,
  onComplete,
  label,
  error,
  disabled,
  autoFocus,
  className,
}: CodeInputProps) {
  const groupId = useId();
  const labelId = `${groupId}-label`;
  const errorId = `${groupId}-error`;
  const boxes = useRef<(HTMLInputElement | null)[]>([]);
  const hasError = Boolean(error);
  // The one box Tab lands on: the first empty one, or the last when full.
  const currentIndex = Math.min(value.length, length - 1);

  function focusBox(index: number) {
    const clamped = Math.max(0, Math.min(index, length - 1));
    boxes.current[clamped]?.focus();
  }

  function commit(next: string, focusIndex: number) {
    const limited = next.slice(0, length);
    if (limited !== value) {
      onChange(limited);
      if (limited.length === length) {
        onComplete?.(limited);
      }
    }
    focusBox(focusIndex);
  }

  function removeAt(index: number) {
    commit(value.slice(0, index) + value.slice(index + 1), index);
  }

  function insertDigits(index: number, digits: string) {
    if (digits.length === 1) {
      commit(
        value.slice(0, index) + digits + value.slice(index + 1),
        index + 1,
      );
      return;
    }
    // A whole code pasted anywhere replaces everything.
    const start = digits.length >= length ? 0 : index;
    commit(value.slice(0, start) + digits, start + digits.length);
  }

  function handleKeyDown(
    index: number,
    event: KeyboardEvent<HTMLInputElement>,
  ) {
    if (event.ctrlKey || event.metaKey || event.altKey) {
      return;
    }
    if (SINGLE_DIGIT.test(event.key)) {
      event.preventDefault();
      insertDigits(index, event.key);
      return;
    }
    switch (event.key) {
      case "Backspace":
        event.preventDefault();
        // On an empty box, Backspace erases the digit before it.
        if (index < value.length) {
          removeAt(index);
        } else if (index > 0) {
          removeAt(index - 1);
        }
        return;
      case "Delete":
        event.preventDefault();
        if (index < value.length) {
          removeAt(index);
        }
        return;
      case "ArrowLeft":
        event.preventDefault();
        focusBox(index - 1);
        return;
      case "ArrowRight":
        event.preventDefault();
        focusBox(index + 1);
        return;
      default:
    }
  }

  // Phone keyboards and autofill type without sending usable key events.
  function handleChange(index: number, event: ChangeEvent<HTMLInputElement>) {
    const digits = event.target.value.replace(NON_DIGITS, "");
    if (digits === "") {
      removeAt(index);
      return;
    }
    const existing = value[index] ?? "";
    // Typing over a filled box leaves two digits; keep only the new one.
    const typed =
      existing !== "" && digits.length > 1
        ? digits.replace(existing, "")
        : digits;
    if (typed !== "") {
      insertDigits(index, typed);
    }
  }

  function handlePaste(index: number, event: ClipboardEvent<HTMLInputElement>) {
    event.preventDefault();
    const digits = event.clipboardData.getData("text").replace(NON_DIGITS, "");
    if (digits !== "") {
      insertDigits(index, digits);
    }
  }

  // A click on a far box lands on the first empty one instead.
  function handleMouseDown(index: number, event: MouseEvent<HTMLInputElement>) {
    if (index > value.length) {
      event.preventDefault();
      focusBox(value.length);
    }
  }

  return (
    <div
      role="group"
      aria-labelledby={labelId}
      aria-describedby={hasError ? errorId : undefined}
      className={[styles.group, className].filter(Boolean).join(" ")}
    >
      <span id={labelId} className={styles.label}>
        {label}
      </span>

      <div className={styles.boxes}>
        {Array.from({ length }, (_, index) => (
          <input
            key={index}
            ref={(element) => {
              boxes.current[index] = element;
            }}
            className={`${styles.box} ${hasError ? styles.invalid : ""}`}
            type="text"
            inputMode="numeric"
            autoComplete={index === 0 ? "one-time-code" : "off"}
            value={value[index] ?? ""}
            tabIndex={index === currentIndex ? 0 : -1}
            disabled={disabled}
            autoFocus={autoFocus && index === 0}
            aria-label={CodeInputText.boxLabel(index + 1, length)}
            aria-invalid={hasError || undefined}
            onChange={(event) => handleChange(index, event)}
            onKeyDown={(event) => handleKeyDown(index, event)}
            onPaste={(event) => handlePaste(index, event)}
            onMouseDown={(event) => handleMouseDown(index, event)}
            onFocus={(event) => event.currentTarget.select()}
          />
        ))}
      </div>

      {error ? (
        <FieldError id={errorId} message={error} className={styles.error} />
      ) : null}
    </div>
  );
}
