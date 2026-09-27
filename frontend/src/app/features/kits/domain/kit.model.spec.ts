import { describe, expect, it } from 'vitest';
import { aProduct } from '@features/catalog/testing';
import { KitDefinition, resolveKits } from './kit.model';

const kit = (productIds: number[]): KitDefinition => ({
  id: 'k', code: 'SSD', goal: 'Meta', name: 'Kit', note: '', question: '', productIds
});

describe('resolveKits', () => {
  it('suma los precios actuales del catálogo', () => {
    const [resolved] = resolveKits([kit([1, 2])], [aProduct({ id: 1, price: 100 }), aProduct({ id: 2, price: 250 })]);
    expect(resolved.total).toBe(350);
    expect(resolved.available).toBe(true);
  });

  it('no está disponible si una pieza está agotada', () => {
    const [resolved] = resolveKits([kit([1, 2])], [aProduct({ id: 1 }), aProduct({ id: 2, stock: 0 })]);
    expect(resolved.available).toBe(false);
  });

  it('no está disponible si falta una pieza (inactiva o borrada)', () => {
    const [resolved] = resolveKits([kit([1, 99])], [aProduct({ id: 1 })]);
    expect(resolved.available).toBe(false);
    expect(resolved.products.map(p => p.id)).toEqual([1]);
  });

  it('descarta kits sin ninguna pieza en el catálogo', () => {
    expect(resolveKits([kit([98, 99])], [aProduct({ id: 1 })])).toEqual([]);
  });
});
