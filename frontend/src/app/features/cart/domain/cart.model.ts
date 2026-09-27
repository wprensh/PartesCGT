import type { Product } from '@features/catalog';

export interface CartItem {
  product: Product;
  quantity: number;
}

/** Nunca más unidades que el stock disponible. */
export function clampQuantity(quantity: number, product: Pick<Product, 'stock'>): number {
  return Math.min(quantity, product.stock);
}

export function cartCount(items: CartItem[]): number {
  return items.reduce((n, i) => n + i.quantity, 0);
}

export function cartTotal(items: CartItem[]): number {
  return items.reduce((sum, i) => sum + i.product.price * i.quantity, 0);
}
