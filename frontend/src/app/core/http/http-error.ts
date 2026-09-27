import { HttpErrorResponse } from '@angular/common/http';

/** Convierte un error HTTP en un mensaje útil para mostrar en pantalla. */
export function describeHttpError(e: HttpErrorResponse): string {
  if (e.status === 0) return 'Sin conexión con la API: verifica que esté corriendo en el puerto 5080.';
  if (e.status === 200) return 'Llegó HTML en vez de JSON: Angular se inició sin el proxy.';
  if (e.status === 401) return 'Tu sesión expiró. Vuelve a iniciar sesión.';
  if (e.status === 403) return 'No tienes permisos para esta acción.';
  if (e.status === 429) return 'Demasiados intentos seguidos. Espera un minuto.';
  const err = e.error;
  if (err?.errors) {
    // Errores de validación de ASP.NET ({ errors: { Campo: ["msg"] } })
    return Object.values(err.errors as Record<string, string[]>).flat().join(' ');
  }
  if (typeof err?.detail === 'string') return err.detail;
  if (typeof err === 'string' && err.length < 300) return err;
  return `Error ${e.status}: ${e.statusText || e.message}`;
}
