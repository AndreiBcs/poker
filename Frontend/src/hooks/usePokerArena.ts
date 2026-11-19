import { useLobbyList } from './useLobbyList';
import { useCallback, useEffect, useRef, useState } from 'react';
import { api, ApiError } from '../api';
import type { LobbySummary, Player, Room, SavedGame } from '../types';
import { useRoomConnection } from './useRoomConnection';

type Modal = { kind: 'join'; lobby: LobbySummary } | { kind: 'raise' | 'leave' | 'abandon' } | null;
const nicknameKey = 'poker.nickname';
const playerKey = 'poker.player';
const roomKey = 'poker.room';
const savedGameKey = 'poker.game';

function storedPlayer(): Player | null {
  try {
    return JSON.parse(sessionStorage.getItem(playerKey) || 'null');
  } catch {
    return null;
  }
}

function storedGame(): SavedGame | null {
  try {
    const game = JSON.parse(localStorage.getItem(savedGameKey) || 'null');
    return typeof game?.gameId === 'string' &&
      typeof game?.name === 'string' &&
      (game.sessionId === null || typeof game.sessionId === 'string')
      ? game
      : null;
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
  const [savedGame, setSavedGame] = useState(storedGame);
  const hasSavedGame = savedGame !== null;
  const [checkingSeat, setCheckingSeat] = useState(!roomId);
  const [room, setRoom] = useState<Room | null>(null);
  const [modal, setModal] = useState<Modal>(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [live, setLive] = useState(false);
  const [raiseChips, setRaiseChips] = useState('');
  const busyRef = useRef(false);
  const activeRoom = useRef(roomId);
  const leaveAttempt = useRef<{ membershipEnded: boolean } | null>(null);

  const rememberGame = useCallback((game: SavedGame | null, expectedGameId?: string | null) => {
    const stored = localStorage.getItem(savedGameKey);
    const current = expectedGameId ? storedGame() : null;
    if (current && current.gameId !== expectedGameId) return;
    if (game) {
      const value = JSON.stringify(game);
      if (stored !== value) localStorage.setItem(savedGameKey, value);
    } else if (stored) {
      localStorage.removeItem(savedGameKey);
    }
    setSavedGame(game);
  }, []);

  const applyRoom = useCallback((snapshot: Room) => {
    if (snapshot.id !== activeRoom.current) return;
    setRoom((previous) =>
      previous?.id === snapshot.id && previous.revision > snapshot.revision ? previous : snapshot,
    );
  }, []);

  function exitRoom(message = '') {
    activeRoom.current = null;
    sessionStorage.removeItem(roomKey);
    setRoomId(null);
    setRoom(null);
    setModal(null);
    setView('lobbies');
    setError(message);
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
    const register = async () => {
      const registered = await api<Player>('/players', 'POST', { nickname });
      try {
        const { player: confirmed } = await api<{ player: Player }>('/players/me');
        if (confirmed.id === registered.id) return confirmed;
      } catch (error) {
        if (error instanceof ApiError && error.status === 404) {
          throw new Error('The running Poker API is out of date. Restart it and try again.');
        }
        if (!(error instanceof ApiError) || error.status !== 401) throw error;
      }
      throw new Error(
        'Poker could not verify your browser seat. Allow first-party cookies and try again.',
      );
    };
    // Serialize first registration across tabs sharing the same cookie jar.
    const current = await (navigator.locks
      ? navigator.locks.request('poker.seat', register)
      : register());
    setPlayer(current);
    setNickname(current.nickname);
    localStorage.setItem(nicknameKey, current.nickname);
    sessionStorage.setItem(playerKey, JSON.stringify(current));
    return current;
  }

  function enter(id: string) {
    leaveAttempt.current = null;
    activeRoom.current = id;
    sessionStorage.setItem(roomKey, id);
    setRoomId(id);
    setModal(null);
  }

  const lobbies = useLobbyList(view === 'lobbies' && !roomId, setError);

  useRoomConnection(roomId, player, applyRoom, setLive, (message) => {
    if (leaveAttempt.current) {
      // Removal can arrive over SignalR before the leave request finishes.
      leaveAttempt.current.membershipEnded = true;
    } else {
      rememberGame(null, roomId);
      exitRoom(message);
    }
  });

  useEffect(() => {
    if (roomId && !player) {
      activeRoom.current = null;
      sessionStorage.removeItem(roomKey);
      setRoomId(null);
    }
  }, [roomId, player]);

  useEffect(() => {
    if (roomId) return;
    let stopped = false;
    let revision = 0;
    const load = async () => {
      const request = ++revision;
      try {
        const state = await api<{ player: Player; savedGame: SavedGame | null }>('/players/me');
        if (stopped || request !== revision) return;
        setPlayer(state.player);
        sessionStorage.setItem(playerKey, JSON.stringify(state.player));
        rememberGame(state.savedGame);
        if (state.savedGame) {
          setNickname(state.player.nickname);
          setView('lobbies');
        }
      } catch (error) {
        if (stopped || request !== revision) return;
        if (error instanceof ApiError && error.status === 401) {
          rememberGame(null);
          sessionStorage.removeItem(playerKey);
          setPlayer(null);
        }
      } finally {
        if (!stopped && request === revision) setCheckingSeat(false);
      }
    };
    const syncGame = (event: StorageEvent) => {
      if (event.key === savedGameKey) void load();
    };
    void load();
    const poll = hasSavedGame ? window.setInterval(load, 2000) : undefined;
    window.addEventListener('focus', load);
    window.addEventListener('storage', syncGame);
    return () => {
      stopped = true;
      window.clearInterval(poll);
      window.removeEventListener('focus', load);
      window.removeEventListener('storage', syncGame);
    };
  }, [roomId, rememberGame, hasSavedGame]);

  useEffect(() => {
    if (!room || room.id !== roomId) return;
    if (room.status === 'finished' && sessionStorage.getItem(roomKey) === room.id) {
      sessionStorage.removeItem(roomKey);
    }
    rememberGame(
      room.status === 'finished'
        ? null
        : {
            gameId: room.id,
            sessionId: room.game?.sessionId ?? null,
            name: room.name,
          },
      room.status === 'finished' ? room.id : undefined,
    );
  }, [room, roomId, rememberGame]);

  async function command(path: string, body?: unknown, method = 'POST') {
    await api(`/lobbies/${roomId}${path}`, method, body);
    applyRoom(await api<Room>(`/lobbies/${roomId}`));
    setModal(null);
  }

  async function createLobby(form: FormData) {
    if (savedGame) throw new Error('Abandon your saved game before creating another lobby.');
    await ensurePlayer();
    const result = await api<{ id: string }>('/lobbies', 'POST', {
      name: form.get('name'),
      password: form.get('password'),
      maxPlayers: Number(form.get('maxPlayers')),
      smallBlind: Number(form.get('smallBlind')),
      bigBlind: Number(form.get('bigBlind')),
    });
    enter(result.id);
  }

  async function joinLobby(id: string, password: FormDataEntryValue | null) {
    if (savedGame) throw new Error('Abandon your saved game before joining another lobby.');
    await ensurePlayer();
    await api(`/lobbies/${id}/join`, 'POST', { password });
    enter(id);
  }

  async function leaveLobby() {
    if (!roomId) return;
    if (room?.game && room.status !== 'finished') {
      await api(`/lobbies/${roomId}/step-away`, 'POST');
      exitRoom();
    } else {
      await removeSeat(roomId);
    }
  }

  async function reconnectGame() {
    if (!savedGame) return;
    const { gameId, sessionId } = savedGame;
    const path = sessionId ? `/games/${gameId}/sessions/${sessionId}` : `/lobbies/${gameId}`;
    try {
      const state = await api<Room>(path);
      if (state.status === 'finished')
        throw new ApiError(404, 'This saved game is no longer active.');
      enter(gameId);
    } catch (error) {
      if (error instanceof ApiError && [401, 403, 404].includes(error.status))
        rememberGame(null, gameId);
      throw error;
    }
  }

  async function abandonGame() {
    if (savedGame) await removeSeat(savedGame.gameId);
  }

  async function removeSeat(id: string) {
    const attempt = { membershipEnded: false };
    leaveAttempt.current = attempt;
    try {
      await api(`/lobbies/${id}/leave`, 'POST');
    } catch (error) {
      const alreadyLeft = error instanceof ApiError && [401, 403, 404].includes(error.status);
      if (!attempt.membershipEnded && !alreadyLeft) {
        leaveAttempt.current = null;
        throw error;
      }
    }
    rememberGame(null, id);
    exitRoom();
  }

  const host = !!room && player?.id === room.ownerId;
  const game = room?.game;
  const myTurn = game?.currentTurnUserId === player?.id;
  const mySeat = game?.players.find((p) => p.id === player?.id);
  const spectating = !!mySeat && mySeat.chips === 0 && !mySeat.isAllIn;
  const toCall = game && mySeat ? game.currentBet - mySeat.currentBet : 0;
  const stack = mySeat?.chips ?? 0;
  const minimumRaiseChips = game?.minimumRaiseChips ?? null;
  const canAct = live && room?.status === 'playing' && myTurn && !busy;

  return {
    createLobby,
    joinLobby,
    leaveLobby,
    reconnectGame,
    abandonGame,
    savedGame,
    checkingSeat,
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
    raiseChips,
    setRaiseChips,
    run,
    ensurePlayer,
    enter,
    command,
    host,
    game,
    myTurn,
    toCall,
    stack,
    minimumRaiseChips,
    canAct,
    spectating,
  };
}

export type PokerArena = ReturnType<typeof usePokerArena>;
