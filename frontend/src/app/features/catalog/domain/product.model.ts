export interface ProductAttribute {
  name: string;
  value: string;
}

export interface Product {
  id: number;
  name: string;
  description: string;
  brand: string;
  /** Foto del producto: subida desde el panel (/api/files/…) o URL http(s). Null si no tiene. */
  imageUrl: string | null;
  categoryId: number;
  category: string;
  /** Presente si el producto está en una subcategoría. */
  parentCategoryId: number | null;
  parentCategory: string | null;
  price: number;
  stock: number;
  isActive: boolean;
  attributes: ProductAttribute[];
  /** Promedio de reseñas, null si no tiene. */
  rating: number | null;
  reviewCount: number;
}

/** Umbral a partir del cual se avisa "Quedan N". */
export const LOW_STOCK = 5;

export type StockStatus = 'out' | 'low' | 'ok';

export function stockStatus(p: Pick<Product, 'stock'>): StockStatus {
  if (p.stock === 0) return 'out';
  return p.stock <= LOW_STOCK ? 'low' : 'ok';
}
