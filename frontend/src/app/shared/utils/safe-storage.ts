/**
 * Acceso a localStorage/sessionStorage que nunca lanza: en modo privado o con el almacenamiento
 * bloqueado, leer devuelve el valor por defecto y escribir se ignora.
 */
export class SafeStorage {
  constructor(private readonly area: () => Storage) {}

  getJson<T>(key: string, fallback: T): T {
    try {
      const raw = this.area().getItem(key);
      return raw == null ? fallback : (JSON.parse(raw) as T);
    } catch {
      return fallback;
    }
  }

  getString(key: string): string | null {
    try { return this.area().getItem(key); } catch { return null; }
  }

  set(key: string, value: unknown): void {
    try { this.area().setItem(key, typeof value === 'string' ? value : JSON.stringify(value)); } catch { /* sin almacenamiento */ }
  }

  remove(key: string): void {
    try { this.area().removeItem(key); } catch { /* sin almacenamiento */ }
  }
}

export const localStore = new SafeStorage(() => localStorage);
export const sessionStore = new SafeStorage(() => sessionStorage);
