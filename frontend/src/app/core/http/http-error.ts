import type { HttpErrorResponse } from '@angular/common/http';
import { isDevMode } from '@angular/core';

/** Estados con los que el proxy (Vercel) avisa que la API no respondió: en el plan gratuito de Render, "despertando". */
const API_UNAVAILABLE = [502, 503, 504];

/**
 * Convierte un error HTTP en un mensaje útil para mostrar en pantalla.
 * En desarrollo incluye pistas técnicas (puerto, proxy); en producción, mensajes para el cliente.
 */
export function describeHttpError(e: HttpErrorResponse, dev = isDevMode()): string {
  if (e.status === 0) {
    return dev ? 'Sin conexión con la API: verifica que esté corriendo en el puerto 5080.'
               : 'No hay conexión con el servidor. Revisa tu internet e intenta de nuevo.';
  }
  if (e.status === 200) {
    // La petición /api devolvió la página (index.html): no hay proxy ni reescritura hacia la API.
    return dev ? 'Llegó HTML en vez de JSON: Angular se inició sin el proxy.'
               : 'La tienda no pudo comunicarse con el servidor. Intenta de nuevo en unos minutos.';
  }
  if (API_UNAVAILABLE.includes(e.status)) return 'El servidor está iniciando. Espera unos segundos y vuelve a intentarlo.';
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
  return dev ? `Error ${e.status}: ${e.statusText || e.message}` : 'Ocurrió un error inesperado. Intenta de nuevo.';
}
