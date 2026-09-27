import { compareText } from '@shared/utils/text';
import { FilterState, isAttributeSelected } from './filter-state';
import { Product } from './product.model';

/**
 * Filtrado y facetas del catálogo. Se calcula en el navegador sobre los productos de la
 * categoría/búsqueda actual: con catálogos de hasta unos miles de productos es instantáneo.
 */

/** Un atributo con un solo producto no ayuda a filtrar y llena el panel. */
const MIN_PRODUCTS_PER_ATTRIBUTE = 2;
const MAX_ATTRIBUTE_GROUPS = 6;
export const RATING_THRESHOLDS = [4, 3] as const;

export interface FacetOption { value: string; count: number; }
export interface AttributeFacet { name: string; options: FacetOption[]; }
export interface Facets {
  price: { min: number; max: number } | null;
  brands: FacetOption[];
  attributes: AttributeFacet[];
  inStock: number;
  ratings: { min: number; count: number }[];
}

type FacetKey = 'price' | 'brand' | 'stock' | 'rating' | `attr:${string}`;

/** ¿Cumple el producto los filtros? `except` ignora un grupo para calcular sus propios conteos. */
function matches(p: Product, f: FilterState, except?: FacetKey): boolean {
  if (except !== 'price') {
    if (f.minPrice != null && p.price < f.minPrice) return false;
    if (f.maxPrice != null && p.price > f.maxPrice) return false;
  }
  if (except !== 'brand' && f.brands.length && !f.brands.includes(p.brand)) return false;
  if (except !== 'stock' && f.inStockOnly && p.stock === 0) return false;
  if (except !== 'rating' && f.minRating != null && (p.rating ?? 0) < f.minRating) return false;

  for (const [name, values] of Object.entries(f.attributes)) {
    if (!values.length || except === `attr:${name}`) continue;
    if (!p.attributes.some(a => a.name === name && values.includes(a.value))) return false;
  }
  return true;
}

export function applyFilters(products: Product[], f: FilterState): Product[] {
  return products.filter(p => matches(p, f));
}

/**
 * Opciones de cada filtro con su conteo. Los conteos de un grupo respetan los demás filtros pero no el
 * propio: así se pueden marcar varias marcas y cada una muestra cuántos productos suma.
 */
export function buildFacets(scope: Product[], f: FilterState): Facets {
  const prices = scope.map(p => p.price);
  const forStock = scope.filter(p => matches(p, f, 'stock'));
  const forRating = scope.filter(p => matches(p, f, 'rating'));

  return {
    price: prices.length ? { min: Math.min(...prices), max: Math.max(...prices) } : null,
    brands: brandFacet(scope, f),
    attributes: attributeFacets(scope, f),
    inStock: forStock.filter(p => p.stock > 0).length,
    ratings: RATING_THRESHOLDS.map(min => ({ min, count: forRating.filter(p => (p.rating ?? 0) >= min).length }))
  };
}

function brandFacet(scope: Product[], f: FilterState): FacetOption[] {
  const brandOf = (p: Product) => (p.brand ? [p.brand] : []);
  return toOptions(
    countBy(scope, brandOf),
    countBy(scope.filter(p => matches(p, f, 'brand')), brandOf),
    f.brands);
}

/** Atributos del alcance actual, los marcados primero y luego los más comunes. */
function attributeFacets(scope: Product[], f: FilterState): AttributeFacet[] {
  const marked = (name: string) => (isAttributeSelected(f, name) ? 1 : 0);

  return [...countBy(scope, p => p.attributes.map(a => a.name)).entries()]
    .filter(([name, count]) => count >= MIN_PRODUCTS_PER_ATTRIBUTE || marked(name))
    .sort((a, b) => marked(b[0]) - marked(a[0]) || b[1] - a[1] || compareText(a[0], b[0]))
    .slice(0, MAX_ATTRIBUTE_GROUPS)
    .map(([name]) => {
      const valuesOf = (p: Product) => p.attributes.filter(a => a.name === name).map(a => a.value);
      return {
        name,
        options: toOptions(
          countBy(scope, valuesOf),
          countBy(scope.filter(p => matches(p, f, `attr:${name}`)), valuesOf),
          f.attributes[name] ?? [])
      };
    });
}

/** Cuántos productos tienen cada clave (un producto cuenta una vez por clave). */
function countBy(products: Product[], keysOf: (p: Product) => string[]): Map<string, number> {
  const counts = new Map<string, number>();
  for (const p of products) {
    for (const key of new Set(keysOf(p))) counts.set(key, (counts.get(key) ?? 0) + 1);
  }
  return counts;
}

/** Todas las opciones del alcance (y las ya marcadas), con el conteo que respeta los demás filtros. */
function toOptions(all: Map<string, number>, visible: Map<string, number>, selected: string[]): FacetOption[] {
  return [...new Set([...all.keys(), ...selected])]
    .map(value => ({ value, count: visible.get(value) ?? 0 }))
    .sort((a, b) => compareText(a.value, b.value));
}
