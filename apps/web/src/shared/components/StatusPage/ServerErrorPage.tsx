// File : /team-samsara/apps/web/src/shared/components/StatusPage/ServerErrorPage.tsx
// Version : 1.0.0
// Latest commit: task/web-error-pages
// Author : Gerrah
// Purpose : The 500 page, with a retry button and a way back home.

import { Button } from "@/shared/components/Button/Button";
import { LinkButton } from "@/shared/components/Button/LinkButton";
import { routePaths } from "@/shared/constants/routePaths";
import { errorPagesContent } from "./errorPagesContent";
import { StatusPage } from "./StatusPage";

interface ServerErrorPageProps {
  /** What the retry button does: reload data, check the session again... */
  onRetry: () => void;
  /** Disables the retry button while the retry is running. */
  isRetrying?: boolean;
}

export function ServerErrorPage({
  onRetry,
  isRetrying = false,
}: ServerErrorPageProps) {
  const text = errorPagesContent.serverError;

  return (
    <StatusPage
      code={text.code}
      title={text.title}
      body={text.body}
      actions={
        <>
          <Button isLoading={isRetrying} onClick={onRetry}>
            {text.retry}
          </Button>
          <LinkButton to={routePaths.home}>{text.home}</LinkButton>
        </>
      }
    />
  );
}
