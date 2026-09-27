import { categoryCode } from './category-families';
import type { Product } from './product.model';

/** Referencia visible del producto: #0007. */
export function productSku(p: Pick<Product, 'id'>): string {
  return `#${p.id.toString().padStart(4, '0')}`;
}

/** Código corto de su categoría; en una subcategoría se usa el del padre (SSD NVMe → SSD). */
export function productCode(p: Pick<Product, 'category' | 'parentCategory'>): string {
  return categoryCode(p.parentCategory ?? p.category);
}

/** "Almacenamiento › SSD NVMe", o solo la categoría si es principal. */
export function categoryPath(p: Pick<Product, 'category' | 'parentCategory'>): string {
  return p.parentCategory ? `${p.parentCategory} › ${p.category}` : p.category;
}
