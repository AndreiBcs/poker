import { useEffect, useRef } from 'react';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { api, ApiError } from '../api';
import type { Player, Room } from '../types';

export function useRoomConnection(
  roomId: string | null, player: Player | null,
  setRoom: (room: Room) => void, setLive: (live: boolean) => void,
  setError: (error: string) => void, exitRoom: () => void,
) {
  const exit = useRef(exitRoom);
  exit.current = exitRoom;

  useEffect(() => {
    if (!roomId || !player) return;
    let stopped = false;
    let retry: ReturnType<typeof setTimeout> | undefined;
    const connection = new HubConnectionBuilder().withUrl('/api/live')
      .configureLogging(LogLevel.None).build();
    connection.on('State', (state: Room) => { if (!stopped) setRoom(state); });
    connection.on('Removed', (message: string) => {
      if (!stopped) { setError(message); exit.current(); }
    });
    const connect = async () => {
      try {
        // Revalidate membership on every reconnect, including after an API restart.
        const state = await api<Room>(`/lobbies/${roomId}`, player.id);
        if (stopped) return;
        setRoom(state);
        await connection.start();
        if (stopped) { await connection.stop(); return; }
        await connection.invoke('JoinRoom', roomId, player.id);
        if (!stopped) setLive(true);
      } catch (error) {
        if (stopped) return;
        await connection.stop();
        if (error instanceof ApiError && [403, 404].includes(error.status)) {
          setError(error.message); exit.current();
        } else schedule();
      }
    };
    function schedule() {
      if (stopped) return;
      setLive(false);
      clearTimeout(retry);
      retry = setTimeout(() => void connect(), 1200);
    }
    connection.onclose(schedule);
    void connect();
    return () => { stopped = true; clearTimeout(retry); void connection.stop(); setLive(false); };
  }, [roomId, player, setRoom, setLive, setError]);
}
