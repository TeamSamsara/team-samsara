// File : /team-samsara/apps/web/src/shared/components/Fields/TextField/TextField.tsx
// Version : 1.3.0
// Latest commit: task/web-toast-messages
// Author : Gerrah
// Purpose : The generic single-line input. Connects its label and error message to the
// input for screen readers; the error shows inside the box, at the right end. What is
// validated and when is decided by the caller.

import { useId, type ComponentProps, type ReactNode } from "react";
import { FieldError } from "../FieldError/FieldError";
import styles from "./TextField.module.css";

export interface TextFieldProps extends ComponentProps<"input"> {
  /** Read by screen readers; the placeholder is what sighted people see. */
  label: string;
  /** The message inside the field. Leave empty when the value is fine. */
  error?: string | null;
  /** Content placed inside the right end of the field (a button, an icon). */
  trailing?: ReactNode;
}

export function TextField({
  label,
  error,
  trailing,
  id,
  className,
  ...rest
}: TextFieldProps) {
  const generatedId = useId();
  const inputId = id ?? generatedId;
  const errorId = `${inputId}-error`;
  const hasError = Boolean(error);

  return (
    <div className={[styles.field, className].filter(Boolean).join(" ")}>
      <label htmlFor={inputId} className={styles.label}>
        {label}
      </label>

      <div className={`${styles.control} ${hasError ? styles.invalid : ""}`}>
        <input
          {...rest}
          id={inputId}
          className={styles.input}
          aria-invalid={hasError || undefined}
          aria-describedby={hasError ? errorId : undefined}
        />
        {error ? (
          <FieldError id={errorId} message={error} className={styles.error} />
        ) : null}
        {trailing}
      </div>
    </div>
  );
}
