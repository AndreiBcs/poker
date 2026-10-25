export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
  ) {
    super(message);
  }
}
export async function api<T>(
  path: string,
  playerId?: string,
  method = 'GET',
  body?: unknown,
): Promise<T> {
  const response = await fetch(`/api${path}`, {
    method,
    headers: {
      ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
      ...(playerId ? { 'X-Player-Id': playerId } : {}),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if ([502, 503, 504].includes(response.status)) {
    throw new ApiError(
      response.status,
      'The game server is unavailable. Please retry in a moment.',
    );
  }
  if (!response.ok) {
    const error = await response.json().catch(() => ({}));
    throw new ApiError(
      response.status,
      error.error || error.title || `Request failed (${response.status}).`,
    );
  }
  return response.status === 204 ? (undefined as T) : response.json();
}
