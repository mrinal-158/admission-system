import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { CLASS_REFRESH_MS, HOLD_REFRESH_MS } from '../config';
import { api } from './client';
import type { CreateClassInput, Hold } from './types';

export const queryKeys = {
  classes: ['classes'] as const,
  hold: (holdId: string) => ['holds', holdId] as const,
  myHolds: (studentId: string) => ['me', studentId, 'holds'] as const,
};

/** Seat counts poll in the background, so they stay fresh without a reload. */
export function useClasses() {
  return useQuery({
    queryKey: queryKeys.classes,
    queryFn: api.listClasses,
    refetchInterval: CLASS_REFRESH_MS,
  });
}

export function useHold(holdId: string) {
  return useQuery({
    queryKey: queryKeys.hold(holdId),
    queryFn: () => api.getHold(holdId),
    // Only a live hold can still change on the server (e.g. confirmed in another tab).
    refetchInterval: (query) => (query.state.data?.status === 'HELD' ? HOLD_REFRESH_MS : false),
  });
}

export function useMyHolds(studentId: string | null) {
  return useQuery({
    queryKey: queryKeys.myHolds(studentId ?? ''),
    queryFn: () => api.myHolds(studentId!),
    enabled: studentId !== null,
    refetchInterval: HOLD_REFRESH_MS,
  });
}

export function useCreateClass() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: CreateClassInput) => api.createClass(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.classes }),
  });
}

/** Shared cache updates after any change to a hold: it and the seat counts are now stale. */
function useHoldMutation<TVars>(mutationFn: (vars: TVars) => Promise<Hold | void>) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn,
    onSuccess: (hold) => {
      if (hold) queryClient.setQueryData(queryKeys.hold(hold.holdId), hold);
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.classes });
      void queryClient.invalidateQueries({ queryKey: ['holds'] });
      void queryClient.invalidateQueries({ queryKey: ['me'] });
    },
  });
}

export const usePlaceHold = () =>
  useHoldMutation(({ classId, studentId }: { classId: string; studentId: string }) => api.placeHold(classId, studentId));

export const useConfirmHold = () =>
  useHoldMutation(({ holdId, studentId }: { holdId: string; studentId: string }) => api.confirmHold(holdId, studentId));

export const useCancelHold = () =>
  useHoldMutation(({ holdId, studentId }: { holdId: string; studentId: string }) => api.cancelHold(holdId, studentId));
