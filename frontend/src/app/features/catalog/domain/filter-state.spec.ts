import { describe, expect, it } from 'vitest';
import {
  EMPTY_FILTERS, FilterState, activeFilterCount, describeActiveFilters, toggleAttribute, toggleBrand, toggleValue
} from './filter-state';

const price = (n: number) => `$${n}`;

describe('toggleValue', () => {
  it('agrega si no está y quita si está', () => {
    expect(toggleValue(['a'], 'b')).toEqual(['a', 'b']);
    expect(toggleValue(['a', 'b'], 'a')).toEqual(['b']);
  });
});

describe('operaciones inmutables', () => {
  it('toggleBrand y toggleAttribute no modifican el estado recibido', () => {
    const before = structuredClone(EMPTY_FILTERS);
    toggleBrand(EMPTY_FILTERS, 'AMD');
    toggleAttribute(EMPTY_FILTERS, 'Socket', 'AM4');
    expect(EMPTY_FILTERS).toEqual(before);
  });
});

describe('activeFilterCount', () => {
  it('cuenta el rango de precio como un solo filtro', () => {
    const f: FilterState = {
      minPrice: 1, maxPrice: 2, brands: ['A', 'B'], attributes: { Capacidad: ['8 GB'], Tipo: [] },
      inStockOnly: true, minRating: 4
    };
    expect(activeFilterCount(f)).toBe(1 + 2 + 1 + 1 + 1);
  });
});

describe('describeActiveFilters', () => {
  it('describe el precio según los extremos definidos', () => {
    expect(describeActiveFilters({ ...EMPTY_FILTERS, minPrice: 10, maxPrice: 20 }, price)[0].label).toBe('$10 – $20');
    expect(describeActiveFilters({ ...EMPTY_FILTERS, minPrice: 10 }, price)[0].label).toBe('Desde $10');
    expect(describeActiveFilters({ ...EMPTY_FILTERS, maxPrice: 20 }, price)[0].label).toBe('Hasta $20');
  });

  it('cada etiqueta sabe quitarse a sí misma sin tocar las demás', () => {
    const f: FilterState = { ...EMPTY_FILTERS, brands: ['AMD', 'Intel'], attributes: { Socket: ['AM4'] }, inStockOnly: true };
    const chips = describeActiveFilters(f, price);
    expect(chips.map(c => c.label)).toEqual(['AMD', 'Intel', 'Socket: AM4', 'Disponibles']);

    const withoutAmd = chips[0].remove(f);
    expect(withoutAmd.brands).toEqual(['Intel']);
    expect(withoutAmd.attributes).toEqual({ Socket: ['AM4'] });
    expect(chips[2].remove(f).attributes).toEqual({ Socket: [] });
  });
});
