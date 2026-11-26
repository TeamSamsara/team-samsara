// File : /team-samsara/apps/web/src/features/contact/ui/SubjectSelect.tsx
// Version : 1.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : The subject list of the contact form. A native select, so every phone and screen
// reader already knows how to use it, drawn like the text fields. The label is read by screen
// readers; the first option is what sighted people see until a subject is chosen.

import { useId } from "react";
import { FieldError } from "@/shared/components/Fields/FieldError/FieldError";
import {
  contactCategoryOrder,
  type ContactCategoryValue,
} from "../constants/contactConstants";
import { ContactCategoryLabels } from "../view/contactPageContent";
import styles from "./SubjectSelect.module.css";

interface SubjectSelectProps {
  readonly label: string;
  readonly value: ContactCategoryValue | "";
  readonly error?: string | null;
  readonly onChange: (value: ContactCategoryValue | "") => void;
  readonly onBlur: () => void;
}

export function SubjectSelect({
  label,
  value,
  error,
  onChange,
  onBlur,
}: SubjectSelectProps) {
  const id = useId();
  const errorId = `${id}-error`;

  return (
    <div className={styles.field}>
      <label htmlFor={id} className={styles.label}>
        {label}
      </label>

      <div className={`${styles.control} ${error ? styles.invalid : ""}`}>
        <select
          id={id}
          name="category"
          className={styles.select}
          value={value}
          aria-invalid={Boolean(error) || undefined}
          aria-describedby={error ? errorId : undefined}
          data-empty={value === "" || undefined}
          onChange={(event) =>
            onChange(event.target.value as ContactCategoryValue | "")
          }
          onBlur={onBlur}
        >
          <option value="" disabled>
            {label}
          </option>
          {contactCategoryOrder.map((category) => (
            <option key={category} value={category}>
              {ContactCategoryLabels[category]}
            </option>
          ))}
        </select>
        {error ? (
          <FieldError id={errorId} message={error} className={styles.error} />
        ) : null}
      </div>
    </div>
  );
}
