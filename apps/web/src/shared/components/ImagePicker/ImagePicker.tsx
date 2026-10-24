// File : /team-samsara/apps/web/src/shared/components/ImagePicker/ImagePicker.tsx
// Version : 1.1.0
// Latest commit: task/web-toast-messages
// Author : Gerrah
// Purpose : Lets a member choose, replace or remove a picture. The frame is a
// circle or a banner, and the controls are either buttons beside the picture
// or a button on it that opens an Upload / Remove menu. The screen owns the
// choice and decides when to upload; nothing is sent from here. A rejected file
// shows as a toast (see useImageChoice); an error passed in by the screen stays
// under the picture until the screen clears it.

import type { ReactNode } from "react";
import { Dropdown } from "@/shared/components/Dropdown/Dropdown";
import { DropdownItem } from "@/shared/components/Dropdown/DropdownItem";
import { DropdownPanel } from "@/shared/components/Dropdown/DropdownPanel";
import { DropdownTrigger } from "@/shared/components/Dropdown/DropdownTrigger";
import { InlineMessage } from "@/shared/components/InlineMessage/InlineMessage";
import { Icon } from "@/shared/icons/Icon";
import type { ImageChange } from "./imageChange";
import styles from "./ImagePicker.module.css";
import { useImageChoice } from "./useImageChoice";

export interface ImagePickerLabels {
  choose: string;
  change: string;
  remove: string;
  menuTrigger: string;
  menuUpload: string;
  menuRemove: string;
}

interface ImagePickerProps {
  shape: "circle" | "banner";
  controls: "inline" | "menu";
  labels: ImagePickerLabels;
  savedUrl: string | null;
  change: ImageChange;
  onChange: (change: ImageChange) => void;
  validate: (file: File) => string | null;
  accept: string;
  placeholder?: ReactNode;
  hint?: string;
  error?: string | null;
}

export function ImagePicker({
  shape,
  controls,
  labels,
  savedUrl,
  change,
  onChange,
  validate,
  accept,
  placeholder,
  hint,
  error,
}: ImagePickerProps) {
  // Destructured on purpose: passing `choice.setInput` straight to `ref` makes
  // the react-hooks/refs lint treat the whole object as a ref.
  const { setInput, shownUrl, hasImage, openFileDialog, choose, remove } =
    useImageChoice({ savedUrl, change, validate, onChange });

  const frame = (
    <div className={styles.frame} aria-hidden="true">
      {shownUrl ? (
        <img className={styles.image} src={shownUrl} alt="" />
      ) : (
        placeholder
      )}
    </div>
  );

  const message = (
    <>
      <div className={styles.errorSlot}>
        <InlineMessage message={error} />
      </div>
      {hint && !error ? <p className={styles.hint}>{hint}</p> : null}
    </>
  );

  const fileInput = (
    <input
      ref={setInput}
      className={styles.input}
      type="file"
      accept={accept}
      onChange={choose}
      tabIndex={-1}
    />
  );

  if (controls === "inline") {
    return (
      <div className={`${styles.picker} ${styles[shape]} ${styles.inline}`}>
        {frame}
        <div className={styles.inlineControls}>
          <div className={styles.buttons}>
            <button
              type="button"
              className={styles.choose}
              onClick={openFileDialog}
            >
              {hasImage ? labels.change : labels.choose}
            </button>
            {hasImage ? (
              <button
                type="button"
                className={styles.removeLink}
                onClick={remove}
              >
                {labels.remove}
              </button>
            ) : null}
          </div>
          {message}
        </div>
        {fileInput}
      </div>
    );
  }

  return (
    <div className={`${styles.picker} ${styles[shape]} ${styles.overlay}`}>
      <div className={styles.stage}>
        {frame}
        <Dropdown className={styles.menu}>
          <DropdownTrigger
            popup="menu"
            className={styles.trigger}
            aria-label={labels.menuTrigger}
          >
            <Icon name="camera" className={styles.triggerIcon} />
            {shape === "banner" ? (
              <span className={styles.triggerText}>{labels.menuTrigger}</span>
            ) : null}
          </DropdownTrigger>
          <DropdownPanel
            role="menu"
            aria-label={labels.menuTrigger}
            placement={shape === "banner" ? "bottom-end" : "bottom-start"}
            offset={10}
            className={styles.panel}
          >
            <DropdownItem
              role="menuitem"
              className={styles.item}
              onSelect={openFileDialog}
            >
              <Icon name="upload" className={styles.itemIcon} />
              {labels.menuUpload}
            </DropdownItem>
            {hasImage ? (
              <>
                <hr className={styles.divider} />
                <DropdownItem
                  role="menuitem"
                  className={`${styles.item} ${styles.danger}`}
                  onSelect={remove}
                >
                  <Icon name="trash" className={styles.itemIcon} />
                  {labels.menuRemove}
                </DropdownItem>
              </>
            ) : null}
          </DropdownPanel>
        </Dropdown>
      </div>
      {message}
      {fileInput}
    </div>
  );
}
