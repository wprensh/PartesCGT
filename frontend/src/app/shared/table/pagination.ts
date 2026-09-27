/** Filas por página en las tablas de mantenimiento: 10 se leen sin desplazarse y cargan rápido. */
export const PAGE_SIZE = 10;

/** Índice de la última página (0 si no hay filas). */
export function lastPageIndex(length: number, pageSize = PAGE_SIZE): number {
  return Math.max(0, Math.ceil(length / pageSize) - 1);
}

/**
 * Mantiene la página dentro del rango: si se elimina la última fila de la última página,
 * se muestra la anterior en vez de una página vacía.
 */
export function clampPageIndex(pageIndex: number, length: number, pageSize = PAGE_SIZE): number {
  return Math.min(Math.max(0, pageIndex), lastPageIndex(length, pageSize));
}

/** Filas de la página pedida (ya ajustada al rango). */
export function pageSlice<T>(items: readonly T[], pageIndex: number, pageSize = PAGE_SIZE): T[] {
  const start = clampPageIndex(pageIndex, items.length, pageSize) * pageSize;
  return items.slice(start, start + pageSize);
}

/** trackBy para MatTable: filas identificadas por su Id (no se re-crean al recargar la lista). */
export const trackById = (_: number, row: { id: number }) => row.id;

/** "11 – 20 de 34", o "0 de 0" sin filas. */
export function rangeLabel(pageIndex: number, pageSize: number, length: number): string {
  if (length === 0 || pageSize === 0) return `0 de ${length}`;
  const start = clampPageIndex(pageIndex, length, pageSize) * pageSize;
  return `${start + 1} – ${Math.min(start + pageSize, length)} de ${length}`;
}
