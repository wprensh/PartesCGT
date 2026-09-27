import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, firstValueFrom, map, of, switchMap, tap } from 'rxjs';
import { sessionStore } from '@shared/utils/safe-storage';
import { isJwtExpired } from './jwt';
import { PermissionCode, Profile } from './permissions';

const TOKEN_KEY = 'tienda.admin.token';

interface LoginResponse { token: string; expiresAt: string; }

/**
 * Sesión del panel. El token vive en sessionStorage (se cierra al cerrar la pestaña).
 * El perfil (nombre, rol y permisos) se pide al backend al entrar y al recargar la página.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private profileRequest: Promise<Profile | null> | null = null;

  readonly token = signal<string | null>(this.restore());
  readonly profile = signal<Profile | null>(null);
  readonly displayName = computed(() => this.profile()?.fullName ?? '');

  isLoggedIn(): boolean {
    const token = this.token();
    if (token && isJwtExpired(token)) {
      this.clear();
      return false;
    }
    return !!token;
  }

  /** ¿El usuario conectado tiene este permiso? (Para mostrar u ocultar opciones; el backend valida igual.) */
  can(permission: PermissionCode): boolean {
    return this.profile()?.permissions.includes(permission) ?? false;
  }

  login(email: string, password: string): Observable<Profile | null> {
    return this.http.post<LoginResponse>('/api/auth/login', { email, password }).pipe(
      tap(({ token }) => this.setToken(token)),
      switchMap(() => this.fetchProfile())
    );
  }

  /** El perfil, pidiéndolo una sola vez aunque varias guardas lo necesiten a la vez. */
  ensureProfile(): Promise<Profile | null> {
    if (this.profile()) return Promise.resolve(this.profile());
    if (!this.isLoggedIn()) return Promise.resolve(null);
    this.profileRequest ??= firstValueFrom(this.fetchProfile()).finally(() => (this.profileRequest = null));
    return this.profileRequest;
  }

  /** Cambia la contraseña; el backend cierra las demás sesiones y devuelve un token nuevo para esta. */
  changePassword(currentPassword: string, newPassword: string) {
    return this.http.post<LoginResponse>('/api/auth/change-password', { currentPassword, newPassword }).pipe(
      tap(({ token }) => this.setToken(token)),
      map(() => undefined)
    );
  }

  logout(expired = false) {
    this.clear();
    this.router.navigate(['/admin/login'], expired ? { queryParams: { expirada: 1 } } : {});
  }

  private fetchProfile(): Observable<Profile | null> {
    return this.http.get<Profile>('/api/auth/me').pipe(
      tap(profile => this.profile.set(profile)),
      catchError(() => of(null))
    );
  }

  private setToken(token: string) {
    this.token.set(token);
    sessionStore.set(TOKEN_KEY, token);
  }

  private clear() {
    this.token.set(null);
    this.profile.set(null);
    sessionStore.remove(TOKEN_KEY);
  }

  private restore(): string | null {
    const token = sessionStore.getString(TOKEN_KEY);
    return token && !isJwtExpired(token) ? token : null;
  }
}
