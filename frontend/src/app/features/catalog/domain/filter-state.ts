/**
 * Estado de los filtros del catálogo y operaciones inmutables sobre él.
 * Todas las funciones devuelven un estado nuevo: nunca mutan el recibido.
 */
export interface FilterState {
  minPrice: number | null;
  maxPrice: number | null;
  brands: string[];
  /** Nombre del atributo → valores marcados. */
  attributes: Record<string, string[]>;
  inStockOnly: boolean;
  minRating: number | null;
}

export const EMPTY_FILTERS: FilterState = {
  minPrice: null, maxPrice: null, brands: [], attributes: {}, inStockOnly: false, minRating: null
};

/** Marca o desmarca un valor en una lista. */
export function toggleValue(list: string[], value: string): string[] {
  return list.includes(value) ? list.filter(v => v !== value) : [...list, value];
}

export function toggleBrand(f: FilterState, brand: string): FilterState {
  return { ...f, brands: toggleValue(f.brands, brand) };
}

export function toggleAttribute(f: FilterState, name: string, value: string): FilterState {
  return { ...f, attributes: { ...f.attributes, [name]: toggleValue(f.attributes[name] ?? [], value) } };
}

export function isAttributeSelected(f: FilterState, name: string, value?: string): boolean {
  const values = f.attributes[name] ?? [];
  return value === undefined ? values.length > 0 : values.includes(value);
}

export function activeFilterCount(f: FilterState): number {
  return (f.minPrice != null || f.maxPrice != null ? 1 : 0)
    + f.brands.length
    + Object.values(f.attributes).reduce((n, values) => n + values.length, 0)
    + (f.inStockOnly ? 1 : 0)
    + (f.minRating != null ? 1 : 0);
}

/** Un filtro activo como etiqueta: su texto y cómo quitarlo del estado. */
export interface ActiveFilter {
  label: string;
  remove: (f: FilterState) => FilterState;
}

export function describeActiveFilters(f: FilterState, formatPrice: (n: number) => string): ActiveFilter[] {
  const chips: ActiveFilter[] = [];

  if (f.minPrice != null || f.maxPrice != null) {
    chips.push({ label: priceLabel(f.minPrice, f.maxPrice, formatPrice), remove: s => ({ ...s, minPrice: null, maxPrice: null }) });
  }
  for (const brand of f.brands) {
    chips.push({ label: brand, remove: s => toggleBrand(s, brand) });
  }
  for (const [name, values] of Object.entries(f.attributes)) {
    for (const value of values) {
      chips.push({ label: `${name}: ${value}`, remove: s => toggleAttribute(s, name, value) });
    }
  }
  if (f.inStockOnly) chips.push({ label: 'Disponibles', remove: s => ({ ...s, inStockOnly: false }) });
  if (f.minRating != null) chips.push({ label: `${f.minRating}★ o más`, remove: s => ({ ...s, minRating: null }) });

  return chips;
}

function priceLabel(min: number | null, max: number | null, format: (n: number) => string): string {
  if (min != null && max != null) return `${format(min)} – ${format(max)}`;
  return min != null ? `Desde ${format(min)}` : `Hasta ${format(max!)}`;
}
