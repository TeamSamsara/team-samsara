// File : /team-samsara/apps/web/src/shared/components/Fields/CodeInput/codeInputContent.ts
// Version : 1.0.0
// Latest commit: task/web-auth-codes-reset
// Author : Gerrah
// Purpose : The text read by screen readers on each box of the code input.

export const CodeInputText = {
  boxLabel: (position: number, total: number): string =>
    `Digit ${position} of ${total}`,
} as const;
