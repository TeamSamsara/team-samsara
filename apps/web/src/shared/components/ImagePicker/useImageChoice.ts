// File : /team-samsara/apps/web/src/shared/components/ImagePicker/useImageChoice.ts
// Version : 1.1.0
// Latest commit: task/web-toast-messages
// Author : Gerrah
// Purpose : The logic behind an image picker: opens the file dialog, checks the
// chosen file (a rejected file shows an error toast), keeps a preview of it, and
// works out which picture to show (the new one, the saved one, or none). Nothing
// is uploaded here.

import { useEffect, useState, type ChangeEvent } from "react";
import { useToast } from "@/shared/components/Toast/useToast";
import { keepImage, type ImageChange } from "./imageChange";

const rejectedTitle = "Image not accepted";

interface UseImageChoiceOptions {
  savedUrl: string | null;
  change: ImageChange;
  validate: (file: File) => string | null;
  onChange: (change: ImageChange) => void;
}

interface Preview {
  file: File;
  url: string;
}

export function useImageChoice({
  savedUrl,
  change,
  validate,
  onChange,
}: UseImageChoiceOptions) {
  const { show } = useToast();
  const [input, setInput] = useState<HTMLInputElement | null>(null);
  const [preview, setPreview] = useState<Preview | null>(null);

  // The preview address is released when it is replaced or the picker goes away.
  useEffect(() => {
    if (!preview) {
      return;
    }

    return () => URL.revokeObjectURL(preview.url);
  }, [preview]);

  function openFileDialog() {
    input?.click();
  }

  function choose(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = "";

    if (!file) {
      return;
    }

    const problem = validate(file);

    if (problem) {
      show({ tone: "error", title: rejectedTitle, detail: problem });
      return;
    }

    setPreview({ file, url: URL.createObjectURL(file) });
    onChange({ kind: "replace", file });
  }

  // A saved picture is removed on save; a freshly chosen one is just forgotten.
  function remove() {
    setPreview(null);
    onChange(savedUrl === null ? keepImage : { kind: "remove" });
  }

  function shownUrl(): string | null {
    switch (change.kind) {
      case "replace":
        return preview?.file === change.file ? preview.url : null;
      case "remove":
        return null;
      default:
        return savedUrl;
    }
  }

  const url = shownUrl();

  return {
    setInput,
    shownUrl: url,
    hasImage: url !== null,
    openFileDialog,
    choose,
    remove,
  };
}
