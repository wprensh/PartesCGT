import { describe, expect, it } from 'vitest';
import { aProduct } from '@features/catalog/testing';
import { cartCount, cartTotal, clampQuantity } from './cart.model';

describe('carrito', () => {
  const items = [
    { product: aProduct({ id: 1, price: 1000 }), quantity: 2 },
    { product: aProduct({ id: 2, price: 500 }), quantity: 3 }
  ];

  it('cuenta unidades, no líneas', () => {
    expect(cartCount(items)).toBe(5);
  });

  it('el total multiplica precio por cantidad', () => {
    expect(cartTotal(items)).toBe(3500);
  });

  it('nunca permite más unidades que el stock', () => {
    expect(clampQuantity(8, { stock: 5 })).toBe(5);
    expect(clampQuantity(2, { stock: 5 })).toBe(2);
  });
});
