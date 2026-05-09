export type TableStatus = 
    'waiting' | 'in_progress' | 'finished'

export interface Table{
    id: string,
    name: string,
    hostId: string,
    maxPlayers: number,
    currentPlayers: number,
    status: TableStatus,
    createdAt: string
}    

export interface CreateTableDto{
    name: string,
    maxPlayers: number
}