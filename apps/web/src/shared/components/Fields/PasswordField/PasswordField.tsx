// File : /team-samsara/apps/web/src/shared/components/Fields/PasswordField/PasswordField.tsx
// Version : 1.0.0
// Latest commit: task/web-auth-screens
// Author : Gerrah
// Purpose : A TextField for passwords with a button that shows or hides what was typed.
// Each field keeps its own show/hide state.

import { useState } from "react";
import { Eye, EyeOff } from "lucide-react";
import { TextField, type TextFieldProps } from "../TextField/TextField";
import styles from "./PasswordField.module.css";
import { PasswordFieldText } from "./passwordFieldContent";

type PasswordFieldProps = Omit<TextFieldProps, "type" | "trailing">;

export function PasswordField(props: PasswordFieldProps) {
  const [isRevealed, setIsRevealed] = useState(false);
  const Icon = isRevealed ? EyeOff : Eye;

  return (
    <TextField
      {...props}
      type={isRevealed ? "text" : "password"}
      trailing={
        <button
          type="button"
          className={styles.toggle}
          aria-label={PasswordFieldText.showPassword}
          aria-pressed={isRevealed}
          onClick={() => setIsRevealed((current) => !current)}
        >
          <Icon className={styles.icon} aria-hidden="true" />
        </button>
      }
    />
  );
}
