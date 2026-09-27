import { Product } from './product.model';

export type SortKey = 'relevance' | 'price-asc' | 'price-desc' | 'rating';

export const SORT_OPTIONS: { key: SortKey; label: string }[] = [
  { key: 'relevance', label: 'Por categoría' },
  { key: 'price-asc', label: 'Menor precio' },
  { key: 'price-desc', label: 'Mayor precio' },
  { key: 'rating', label: 'Mejor valorados' }
];

const COMPARERS: Record<SortKey, ((a: Product, b: Product) => number) | null> = {
  // El backend ya los entrega por categoría y nombre.
  relevance: null,
  'price-asc': (a, b) => a.price - b.price,
  'price-desc': (a, b) => b.price - a.price,
  rating: (a, b) => (b.rating ?? 0) - (a.rating ?? 0) || b.reviewCount - a.reviewCount
};

export function sortProducts(products: Product[], key: SortKey): Product[] {
  const compare = COMPARERS[key];
  return compare ? [...products].sort(compare) : products;
}
