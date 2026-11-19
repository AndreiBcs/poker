import { useEffect, useRef } from 'react';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { api, ApiError } from '../api';
import type { Player, Room } from '../types';

export function useRoomConnection(
  roomId: string | null,
  player: Player | null,
  setRoom: (room: Room) => void,
  setLive: (live: boolean) => void,
  onMembershipEnded: (message: string) => void,
) {
  const membershipEnded = useRef(onMembershipEnded);
  membershipEnded.current = onMembershipEnded;

  useEffect(() => {
    if (!roomId || !player) return;
    let stopped = false;
    let retry: ReturnType<typeof setTimeout> | undefined;
    const connection = new HubConnectionBuilder()
      .withUrl('/api/live')
      .configureLogging(LogLevel.None)
      .build();
    connection.on('State', (state: Room) => {
      if (!stopped) setRoom(state);
    });
    connection.on('Removed', endMembership);
    const connect = async () => {
      try {
        // Revalidate membership on every reconnect, including after an API restart.
        const state = await api<Room>(`/lobbies/${roomId}`);
        if (stopped) return;
        setRoom(state);
        if (state.status === 'finished') return;
        await connection.start();
        if (stopped) {
          await connection.stop();
          return;
        }
        await connection.invoke('JoinRoom', roomId);
        if (!stopped) setLive(true);
      } catch (error) {
        if (stopped) return;
        if (error instanceof ApiError && [401, 403, 404].includes(error.status)) {
          endMembership(error.message);
          return;
        }
        await connection.stop();
        schedule();
      }
    };

    function endMembership(message: string) {
      if (stopped) return;
      stop();
      membershipEnded.current(message);
    }

    function schedule() {
      if (stopped) return;
      setLive(false);
      clearTimeout(retry);
      retry = setTimeout(() => void connect(), 1200);
    }
    function stop() {
      if (stopped) return;
      stopped = true;
      clearTimeout(retry);
      void connection.stop();
      setLive(false);
    }

    connection.onclose(schedule);
    void connect();
    return stop;
  }, [roomId, player, setRoom, setLive]);
}
