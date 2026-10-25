import { useEffect, useState } from 'react';
import { api } from '../api';
import type { LobbySummary } from '../types';

export function useLobbyList(enabled: boolean, setError: (message: string) => void) {
  const [lobbies, setLobbies] = useState<LobbySummary[]>([]);
  useEffect(() => {
    if (!enabled) return;
    let stopped = false;
    const load = async () => {
      try {
        const result = await api<LobbySummary[]>('/lobbies');
        if (!stopped) setLobbies(result);
      } catch (e) {
        if (!stopped) setError(e instanceof Error ? e.message : 'Cannot load lobbies.');
      }
    };
    void load();
    const timer = setInterval(() => void load(), 2000);
    return () => {
      stopped = true;
      clearInterval(timer);
    };
  }, [enabled, setError]);

  return lobbies;
}
