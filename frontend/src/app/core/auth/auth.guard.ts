import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { PermissionCode } from './permissions';

// Nota: en guardas async, todo inject() va ANTES del primer await (después se pierde el contexto de inyección).

/** Deja pasar solo con sesión; si no, manda al login y recuerda a dónde volver. Carga el perfil si falta. */
export const adminGuard: CanActivateFn = async (_route, state) => {
  const auth = inject(AuthService);
  const toLogin = inject(Router).createUrlTree(['/admin/login'], { queryParams: { volver: state.url } });
  if (!auth.isLoggedIn()) return toLogin;
  return (await auth.ensureProfile()) ? true : toLogin;
};

/** Exige un permiso para entrar a una sección; sin él, va a la página de "sin acceso". */
export function requirePermission(permission: PermissionCode): CanActivateFn {
  return async () => {
    const auth = inject(AuthService);
    const router = inject(Router);
    await auth.ensureProfile();
    return auth.can(permission) || router.createUrlTree(['/admin/sin-acceso']);
  };
}
