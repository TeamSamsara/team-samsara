// File : /team-samsara/apps/web/src/features/contact/ui/AttachFiles.tsx
// Version : 1.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : The attach button of the contact form and the list of the files chosen, each with
// a remove button. The file picker stays hidden; the button opens it. Which files are fine is
// decided by the form, not here.

import { useRef } from "react";
import { InlineMessage } from "@/shared/components/InlineMessage/InlineMessage";
import {
  contactFileAccept,
  contactLimits,
} from "../constants/contactConstants";
import { ContactPageText } from "../view/contactPageContent";
import styles from "./AttachFiles.module.css";

const bytesPerMegabyte = 1024 * 1024;

interface AttachFilesProps {
  readonly files: readonly File[];
  readonly error?: string | null;
  readonly onAdd: (files: readonly File[]) => void;
  readonly onRemove: (file: File) => void;
}

export function AttachFiles({
  files,
  error,
  onAdd,
  onRemove,
}: AttachFilesProps) {
  const pickerRef = useRef<HTMLInputElement>(null);

  return (
    <div className={styles.attach}>
      <input
        ref={pickerRef}
        type="file"
        name="files"
        multiple
        accept={contactFileAccept}
        className={styles.picker}
        tabIndex={-1}
        aria-hidden="true"
        onChange={(event) => {
          onAdd(Array.from(event.target.files ?? []));
          // The same file can be chosen again after it was removed.
          event.target.value = "";
        }}
      />

      <button
        type="button"
        className={styles.button}
        onClick={() => pickerRef.current?.click()}
      >
        <span aria-hidden="true">+ </span>
        {ContactPageText.attach}
      </button>

      <p className={styles.hint}>
        {ContactPageText.attachHint(
          contactLimits.maxFiles,
          contactLimits.maxFileBytes / bytesPerMegabyte,
        )}
      </p>

      {files.length > 0 ? (
        <ul className={styles.list}>
          {files.map((file) => (
            <li key={`${file.name}-${file.size}`} className={styles.item}>
              <span className={styles.name}>{file.name}</span>
              <button
                type="button"
                className={styles.remove}
                aria-label={ContactPageText.removeFile(file.name)}
                onClick={() => onRemove(file)}
              >
                ×
              </button>
            </li>
          ))}
        </ul>
      ) : null}

      <InlineMessage message={error} withIcon />
    </div>
  );
}
