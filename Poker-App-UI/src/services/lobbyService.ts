import { useAuthStore } from '@/stores/auth'

const BASE_URL = 'https://your-game-service/api'

// --- Types ---
export type RoomStatus = 'waiting' | 'inProgress'

export interface Room {
  id: string
  name: string
  playerCount: number
  maxPlayers: number
  status: RoomStatus
}

export interface CreateRoomRequest {
  name: string
  maxPlayers: number
}

// --- Helpers ---
function authHeaders(): HeadersInit {
  const auth = useAuthStore()
  return {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${auth.token}`
  }
}

async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const data = await response.json().catch(() => ({}))
    throw new Error(data.message ?? `Request failed: ${response.status}`)
  }
  return response.json()
}

// --- API calls ---
export async function getRooms(): Promise<Room[]> {
  const response = await fetch(`${BASE_URL}/lobby`, {
    headers: authHeaders()
  })
  return handleResponse<Room[]>(response)
}

export async function createRoom(request: CreateRoomRequest): Promise<Room> {
  const response = await fetch(`${BASE_URL}/lobby`, {
    method: 'POST',
    headers: authHeaders(),
    body: JSON.stringify(request)
  })
  return handleResponse<Room>(response)
}

export async function joinRoom(roomId: string): Promise<void> {
  const response = await fetch(`${BASE_URL}/lobby/${roomId}/join`, {
    method: 'POST',
    headers: authHeaders()
  })
  return handleResponse<void>(response)
}