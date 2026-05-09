export interface User{
    id: string,
    username: string,
    email: string
}

export interface AuthResponse{
    id: string,
    username: string,
    email: string,
    createdAt: string,
    token: string
}

export interface LoginDto{
    email: string,
    password: string
}

export interface SignUpDto{
    username: string,
    email: string,
    password: string
}

export interface UpdateUserDto{
    username?: string | null,
    email?: string | null,
    password?: string | null
}

export interface ValidationProblemDetails{
    type?: string | null,
    title?: string | null,
    status?: number | null,
    detail?: string | null,
    errors?: Record<string, string[]>
}