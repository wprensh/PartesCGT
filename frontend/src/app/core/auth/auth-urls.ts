/** El login no lleva token (todavía no hay) y su 401 significa "credenciales incorrectas", no "sesión vencida". */
export const LOGIN_URL = '/api/auth/login';

/** ¿Esta petición debe llevar el token del panel? Todas las de /api, incluidas /api/auth/me y cambio de contraseña. */
export function shouldAttachToken(url: string): boolean {
  return url.startsWith('/api/') && url !== LOGIN_URL;
}
