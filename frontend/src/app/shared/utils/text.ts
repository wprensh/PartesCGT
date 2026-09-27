/** Minúsculas y sin tildes, para comparar textos: "Gráficas " → "graficas". */
export function normalize(text: string): string {
  return text.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase().trim();
}

/** Texto apto para URL: "Tarjetas madre" → "tarjetas-madre". */
export function slugify(text: string): string {
  return normalize(text).replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
}

/** Orden alfabético en español que entiende números: "8 GB" antes que "16 GB". */
export function compareText(a: string, b: string): number {
  return a.localeCompare(b, 'es', { numeric: true, sensitivity: 'base' });
}
