// File : /team-samsara/apps/web/src/shared/components/StatusPage/ForbiddenPage.tsx
// Version : 1.0.0
// Latest commit: task/web-error-pages
// Author : Gerrah
// Purpose : The 403 page, for a signed-in member without the needed access level.

import { LinkButton } from "@/shared/components/Button/LinkButton";
import { routePaths } from "@/shared/constants/routePaths";
import { errorPagesContent } from "./errorPagesContent";
import { StatusPage } from "./StatusPage";

export function ForbiddenPage() {
  const text = errorPagesContent.forbidden;

  return (
    <StatusPage
      code={text.code}
      title={text.title}
      body={text.body}
      actions={<LinkButton to={routePaths.home}>{text.home}</LinkButton>}
    />
  );
}
