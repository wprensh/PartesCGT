export interface JwtPayload { name?: string; role?: string; exp?: number; }

/** Lee el payload de un JWT sin validarlo (la validación la hace el backend). */
export function decodeJwt(token: string): JwtPayload | null {
  try {
    const b64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    return JSON.parse(atob(b64.padEnd(b64.length + (4 - (b64.length % 4)) % 4, '=')));
  } catch {
    return null;
  }
}

export function isJwtExpired(token: string): boolean {
  const exp = decodeJwt(token)?.exp;
  return !exp || exp * 1000 <= Date.now();
}
