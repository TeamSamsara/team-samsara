// File : /team-samsara/apps/web/src/shared/components/Toast/toastTypes.ts
// Version : 1.0.0
// Latest commit: task/web-toast-messages
// Author : Gerrah
// Purpose : Types for toast messages: the tone, what a caller sends and what the viewport shows.

export type ToastTone = "success" | "info" | "warning" | "error";

export interface ToastInput {
  tone: ToastTone;
  title: string;
  detail?: string;
}

export interface ToastItem extends ToastInput {
  id: string;
  durationMs: number;
  isLeaving: boolean;
}

export interface ToastActions {
  show: (input: ToastInput) => string;
  dismiss: (id: string) => void;
  remove: (id: string) => void;
}
