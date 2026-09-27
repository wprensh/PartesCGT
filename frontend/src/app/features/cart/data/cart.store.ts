import { Injectable, computed, effect, signal } from '@angular/core';
import type { Product } from '@features/catalog';
import { localStore } from '@shared/utils/safe-storage';
import { CartItem, cartCount, cartTotal, clampQuantity } from '../domain/cart.model';

const STORAGE_KEY = 'tienda.carrito';

/** Carrito del cliente. Persiste en localStorage para sobrevivir recargas. */
@Injectable({ providedIn: 'root' })
export class CartStore {
  readonly items = signal<CartItem[]>(localStore.getJson<CartItem[]>(STORAGE_KEY, []));
  readonly count = computed(() => cartCount(this.items()));
  readonly total = computed(() => cartTotal(this.items()));

  constructor() {
    effect(() => localStore.set(STORAGE_KEY, this.items()));
  }

  add(product: Product, quantity = 1) {
    this.items.update(items => {
      const existing = items.find(i => i.product.id === product.id);
      if (!existing) return [...items, { product, quantity: clampQuantity(quantity, product) }];
      return items.map(i => i.product.id === product.id
        ? { ...i, quantity: clampQuantity(i.quantity + quantity, product) }
        : i);
    });
  }

  addMany(products: Product[]) {
    products.forEach(p => this.add(p));
  }

  setQuantity(productId: number, quantity: number) {
    if (quantity <= 0) return this.remove(productId);
    this.items.update(items => items.map(i => i.product.id === productId
      ? { ...i, quantity: clampQuantity(quantity, i.product) }
      : i));
  }

  remove(productId: number) {
    this.items.update(items => items.filter(i => i.product.id !== productId));
  }

  clear() {
    this.items.set([]);
  }
}
