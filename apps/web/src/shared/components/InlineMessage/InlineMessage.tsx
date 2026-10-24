// File : /team-samsara/apps/web/src/shared/components/InlineMessage/InlineMessage.tsx
// Version : 1.1.0
// Latest commit: task/web-toast-messages
// Author : Gerrah
// Purpose : An error line for a form or modal, with the same icon, size and colour as a field's error. Give it a
// message and it grows in; give it nothing and it collapses, then leaves the page, so a closed form
// keeps exactly the layout it had before.

import { useState, type AnimationEvent } from "react";
import { Icon } from "@/shared/icons/Icon";
import styles from "./InlineMessage.module.css";

interface InlineMessageProps {
  message: string | null | undefined;
  withIcon?: boolean;
  className?: string;
}

export function InlineMessage({
  message,
  withIcon = true,
  className,
}: InlineMessageProps) {
  const [lastMessage, setLastMessage] = useState(message ?? "");

  if (message && message !== lastMessage) setLastMessage(message);

  const text = message || lastMessage;
  if (!text) return null;

  const isClosing = !message;
  const textClassName = className ? `${styles.text} ${className}` : styles.text;

  function handleCollapseEnd(event: AnimationEvent<HTMLDivElement>) {
    const isOwnAnimation = event.target === event.currentTarget;
    if (isOwnAnimation && isClosing) setLastMessage("");
  }

  return (
    <div
      className={styles.collapse}
      data-closing={isClosing}
      role={isClosing ? undefined : "alert"}
      onAnimationEnd={handleCollapseEnd}
    >
      <div className={styles.reveal}>
        <p className={textClassName}>
          {withIcon ? <Icon name="error" className={styles.icon} /> : null}
          {text}
        </p>
      </div>
    </div>
  );
}
