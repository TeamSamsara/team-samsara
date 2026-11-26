// File : /team-samsara/apps/web/src/features/account/hooks/useSaveProfile.ts
// Version : 1.0.0
// Latest commit: task/web-account-settings
// Author : Gerrah
// Purpose : Saves the edit-profile form and keeps the shared profile cache in
// step with the result. The cache is refreshed after every attempt, even a
// failed one, because the first steps of a save may already have gone through.

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useAuth } from "@/app/useAuth";
import {
  saveProfileChanges,
  type ProfileSaveInput,
} from "../service/profileSaveService";
import { myProfileQueryKey } from "./useMyProfile";

export function useSaveProfile() {
  const queryClient = useQueryClient();
  const { userId } = useAuth();
  const queryKey = myProfileQueryKey(userId);

  const mutation = useMutation({
    mutationFn: (input: ProfileSaveInput) => saveProfileChanges(input),
    onSuccess: (attempt) => {
      if (attempt.kind === "saved" && attempt.profile !== null) {
        queryClient.setQueryData(queryKey, attempt.profile);
      }
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey }),
  });

  return { save: mutation.mutateAsync, isSaving: mutation.isPending };
}
