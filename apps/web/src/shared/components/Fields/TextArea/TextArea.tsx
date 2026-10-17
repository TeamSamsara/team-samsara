// File : /team-samsara/apps/web/src/shared/components/Fields/TextArea/TextArea.tsx
// Version : 1.0.0
// Latest commit: task/web-auth-onboarding-navbar
// Author : Gerrah
// Purpose : The generic multi-line input. Looks like TextField and connects
// its label and error message to the field for screen readers; what is
// validated and when is decided by the caller.

import { useId, type ComponentProps } from "react";
import styles from "./TextArea.module.css";

export interface TextAreaProps extends ComponentProps<"textarea"> {
  /** Read by screen readers; the placeholder is what sighted people see. */
  label: string;
  /** The message under the field. Leave empty when the value is fine. */
  error?: string | null;
}

export function TextArea({
  label,
  error,
  id,
  className,
  ...rest
}: TextAreaProps) {
  const generatedId = useId();
  const inputId = id ?? generatedId;
  const errorId = `${inputId}-error`;
  const hasError = Boolean(error);

  return (
    <div className={[styles.field, className].filter(Boolean).join(" ")}>
      <label htmlFor={inputId} className={styles.label}>
        {label}
      </label>

      <textarea
        {...rest}
        id={inputId}
        className={`${styles.input} ${hasError ? styles.invalid : ""}`}
        aria-invalid={hasError || undefined}
        aria-describedby={hasError ? errorId : undefined}
      />

      {hasError ? (
        <p id={errorId} className={styles.error} role="alert">
          {error}
        </p>
      ) : null}
    </div>
  );
}
