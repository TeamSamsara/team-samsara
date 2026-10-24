// File : /team-samsara/apps/web/src/shared/components/Fields/FieldError/FieldError.tsx
// Version : 1.0.0
// Latest commit: task/web-toast-messages
// Author : Gerrah
// Purpose : The error message of a field: an icon and the text, announced to screen readers. Each
// field passes the id its input points to and a class that places the message.

import { Icon } from "@/shared/icons/Icon";
import styles from "./FieldError.module.css";

interface FieldErrorProps {
  id: string;
  message: string;
  className?: string;
}

export function FieldError({ id, message, className }: FieldErrorProps) {
  const classNames = className ? `${styles.error} ${className}` : styles.error;

  return (
    <p id={id} className={classNames} role="alert">
      <Icon name="error" className={styles.icon} />
      <span className={styles.text}>{message}</span>
    </p>
  );
}
