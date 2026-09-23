// File : /team-samsara/apps/web/src/features/ping/service/usePing.ts
// Version : 1.0.0
// Latest commit: feature/apps-web-foundation
// Author : Gerrah
// Purpose : TanStack Query hook wrapping pingService.fetchPing. Produces the
// {isLoading, isError, data} shape every feature's components consume —
// this is the actual mechanism behind that pattern, not just a convention.

import { useQuery } from "@tanstack/react-query";
import { fetchPing } from "./pingService";
import { pingQueryKeys } from "../constants/pingQueryKeys";

export function usePing() {
  return useQuery({
    queryKey: pingQueryKeys.ping,
    queryFn: fetchPing,
  });
}
