// File : /team-samsara/apps/web/src/shared/components/StatusPage/NotFoundPage.tsx
// Version : 1.0.0
// Latest commit: task/web-error-pages
// Author : Gerrah
// Purpose : The 404 page, for an unknown address or a missing member.

import { LinkButton } from "@/shared/components/Button/LinkButton";
import { routePaths } from "@/shared/constants/routePaths";
import { errorPagesContent } from "./errorPagesContent";
import { StatusPage } from "./StatusPage";

export function NotFoundPage() {
  const text = errorPagesContent.notFound;

  return (
    <StatusPage
      code={text.code}
      title={text.title}
      body={text.body}
      actions={<LinkButton to={routePaths.home}>{text.home}</LinkButton>}
    />
  );
}
