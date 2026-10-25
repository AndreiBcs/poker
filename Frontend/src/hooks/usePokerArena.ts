import { useLobbyList } from './useLobbyList';
import { useEffect, useRef, useState } from 'react';
import { api, ApiError } from '../api';
import type { LobbySummary, Player, Room } from '../types';
import { useRoomConnection } from './useRoomConnection';

type Modal = { kind: 'join'; lobby: LobbySummary } | { kind: 'raise' } | { kind: 'leave' } | null;
const nicknameKey = 'poker.nickname';
const playerKey = 'poker.player';
const roomKey = 'poker.room';

function storedPlayer(): Player | null {
  try {
    return JSON.parse(sessionStorage.getItem(playerKey) || 'null');
  } catch {
    return null;
  }
}

export function usePokerArena() {
  const [nickname, setNickname] = useState(localStorage.getItem(nicknameKey) || '');
  const [editingNickname, setEditingNickname] = useState(!localStorage.getItem(nicknameKey));
  const [player, setPlayer] = useState<Player | null>(storedPlayer);
  const [view, setView] = useState<'home' | 'lobbies'>('home');
  const [roomId, setRoomId] = useState<string | null>(sessionStorage.getItem(roomKey));
  const [room, setRoom] = useState<Room | null>(null);
  const [modal, setModal] = useState<Modal>(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [live, setLive] = useState(false);
  const [raise, setRaise] = useState(50);
  const busyRef = useRef(false);

  function exitRoom() {
    sessionStorage.removeItem(roomKey);
    setRoomId(null);
    setRoom(null);
    setModal(null);
    setView('lobbies');
  }

  async function run(work: () => Promise<void>) {
    if (busyRef.current) return;
    busyRef.current = true;
    setBusy(true);
    setError('');
    try {
      await work();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Something went wrong. Please retry.');
    } finally {
      busyRef.current = false;
      setBusy(false);
    }
  }

  async function ensurePlayer() {
    let current = player;
    if (current && current.nickname === nickname.trim()) {
      try {
        await api(`/players/${current.id}`);
        return current;
      } catch (e) {
        if (!(e instanceof ApiError) || e.status !== 404) throw e;
      }
    }
    current = await api<Player>('/players', undefined, 'POST', { nickname });
    setPlayer(current);
    localStorage.setItem(nicknameKey, current.nickname);
    sessionStorage.setItem(playerKey, JSON.stringify(current));
    return current;
  }

  function enter(id: string) {
    sessionStorage.setItem(roomKey, id);
    setRoomId(id);
    setModal(null);
  }

  const lobbies = useLobbyList(view === 'lobbies' && !roomId, setError);

  useRoomConnection(roomId, player, setRoom, setLive, setError, exitRoom);

  // Refreshing a tab preserves its seat; nickname persists across browser visits.
  useEffect(() => {
    if (roomId && !player) {
      sessionStorage.removeItem(roomKey);
      setRoomId(null);
    }
  }, [roomId, player]);

  async function command(path: string, body?: unknown, method = 'POST') {
    await api(`/lobbies/${roomId}${path}`, player!.id, method, body);
    setRoom(await api<Room>(`/lobbies/${roomId}`, player!.id));
    setModal(null);
  }

  async function createLobby(form: FormData) {
    const current = await ensurePlayer();
    const result = await api<{ id: string }>('/lobbies', current.id, 'POST', {
      name: form.get('name'),
      password: form.get('password'),
      maxPlayers: Number(form.get('maxPlayers')),
      smallBlind: Number(form.get('smallBlind')),
      bigBlind: Number(form.get('bigBlind')),
    });
    enter(result.id);
  }

  async function joinLobby(id: string, password: FormDataEntryValue | null) {
    const current = await ensurePlayer();
    await api(`/lobbies/${id}/join`, current.id, 'POST', { password });
    enter(id);
  }

  async function leaveLobby() {
    await api(`/lobbies/${roomId}/leave`, player!.id, 'POST');
    exitRoom();
  }

  const host = !!room && player?.id === room.ownerId;
  const game = room?.game;
  const myTurn = game?.currentTurnUserId === player?.id;
  const mySeat = game?.players.find((p) => p.id === player?.id);
  const toCall = game && mySeat ? game.currentBet - mySeat.currentBet : 0;
  const maxRaise = mySeat ? mySeat.chips - toCall : 0;
  const canAct = live && room?.status === 'playing' && myTurn && !busy;

  return {
    createLobby,
    joinLobby,
    leaveLobby,
    nickname,
    setNickname,
    editingNickname,
    setEditingNickname,
    player,
    view,
    setView,
    roomId,
    room,
    lobbies,
    modal,
    setModal,
    error,
    setError,
    busy,
    live,
    raise,
    setRaise,
    exitRoom,
    run,
    ensurePlayer,
    enter,
    command,
    host,
    game,
    myTurn,
    toCall,
    maxRaise,
    canAct,
  };
}

export type PokerArena = ReturnType<typeof usePokerArena>;
