import { describe, expect, it } from 'vitest';
import { applyFilters, buildFacets } from './facets';
import { EMPTY_FILTERS, FilterState } from './filter-state';
import { aProduct } from './testing/product.fixture';

const ssdKingston = aProduct({
  id: 1, brand: 'Kingston', price: 245_000, rating: 4.7, reviewCount: 3,
  attributes: [{ name: 'Capacidad', value: '1 TB' }, { name: 'Formato', value: 'M.2' }]
});
const ssdCrucial = aProduct({
  id: 2, brand: 'Crucial', price: 135_000, rating: 3.7, reviewCount: 3,
  attributes: [{ name: 'Capacidad', value: '480 GB' }, { name: 'Formato', value: '2,5"' }]
});
const ramKingston = aProduct({
  id: 3, brand: 'Kingston', price: 189_000, stock: 0,
  attributes: [{ name: 'Capacidad', value: '16 GB' }]
});
const teclado = aProduct({ id: 4, brand: '', price: 165_000, attributes: [{ name: 'Color', value: 'Negro' }] });
const catalog = [ssdKingston, ssdCrucial, ramKingston, teclado];

const withFilters = (changes: Partial<FilterState>): FilterState => ({ ...EMPTY_FILTERS, ...changes });
const ids = (list: { id: number }[]) => list.map(p => p.id);

describe('applyFilters', () => {
  it('sin filtros devuelve todo', () => {
    expect(ids(applyFilters(catalog, EMPTY_FILTERS))).toEqual([1, 2, 3, 4]);
  });

  it('el rango de precio incluye los extremos', () => {
    expect(ids(applyFilters(catalog, withFilters({ minPrice: 165_000, maxPrice: 245_000 })))).toEqual([1, 3, 4]);
  });

  it('varias marcas se combinan con O', () => {
    expect(ids(applyFilters(catalog, withFilters({ brands: ['Kingston', 'Crucial'] })))).toEqual([1, 2, 3]);
  });

  it('grupos distintos se combinan con Y', () => {
    const f = withFilters({ brands: ['Kingston'], attributes: { Capacidad: ['16 GB'] } });
    expect(ids(applyFilters(catalog, f))).toEqual([3]);
  });

  it('"solo disponibles" excluye productos sin stock', () => {
    expect(ids(applyFilters(catalog, withFilters({ inStockOnly: true })))).toEqual([1, 2, 4]);
  });

  it('la calificación mínima trata "sin reseñas" como 0', () => {
    expect(ids(applyFilters(catalog, withFilters({ minRating: 4 })))).toEqual([1]);
  });
});

describe('buildFacets', () => {
  it('el conteo de una marca ignora las marcas ya marcadas (para poder sumar otras)', () => {
    const facets = buildFacets(catalog, withFilters({ brands: ['Kingston'] }));
    expect(facets.brands).toEqual([{ value: 'Crucial', count: 1 }, { value: 'Kingston', count: 2 }]);
  });

  it('el conteo de un grupo sí respeta los filtros de otros grupos', () => {
    const facets = buildFacets(catalog, withFilters({ brands: ['Kingston'] }));
    const capacidad = facets.attributes.find(a => a.name === 'Capacidad')!;
    expect(capacidad.options).toEqual([
      { value: '1 TB', count: 1 }, { value: '16 GB', count: 1 }, { value: '480 GB', count: 0 }
    ]);
  });

  it('ordena los valores entendiendo números (8 GB antes que 16 GB)', () => {
    const facets = buildFacets([aProduct({ attributes: [{ name: 'Capacidad', value: '16 GB' }] }),
      aProduct({ id: 2, attributes: [{ name: 'Capacidad', value: '8 GB' }] })], EMPTY_FILTERS);
    expect(facets.attributes[0].options.map(o => o.value)).toEqual(['8 GB', '16 GB']);
  });

  it('oculta atributos con un solo producto, salvo que estén marcados', () => {
    expect(buildFacets(catalog, EMPTY_FILTERS).attributes.map(a => a.name)).not.toContain('Color');
    const marked = buildFacets(catalog, withFilters({ attributes: { Color: ['Negro'] } }));
    expect(marked.attributes[0].name).toBe('Color');
  });

  it('no incluye marcas vacías', () => {
    expect(buildFacets(catalog, EMPTY_FILTERS).brands.map(b => b.value)).not.toContain('');
  });

  it('calcula el rango de precios y los conteos de disponibilidad y calificación', () => {
    const facets = buildFacets(catalog, EMPTY_FILTERS);
    expect(facets.price).toEqual({ min: 135_000, max: 245_000 });
    expect(facets.inStock).toBe(3);
    expect(facets.ratings).toEqual([{ min: 4, count: 1 }, { min: 3, count: 2 }]);
  });

  it('sin productos no hay rango de precios', () => {
    expect(buildFacets([], EMPTY_FILTERS).price).toBeNull();
  });
});
