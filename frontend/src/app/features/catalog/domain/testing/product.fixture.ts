import { Product } from '../product.model';

/** Producto de prueba con valores por defecto; se sobrescribe solo lo que importa en cada caso. */
export function aProduct(overrides: Partial<Product> = {}): Product {
  return {
    id: 1,
    name: 'Producto',
    description: 'Descripción de prueba',
    brand: '',
    imageUrl: null,
    categoryId: 1,
    category: 'Categoría',
    parentCategoryId: null,
    parentCategory: null,
    price: 100_000,
    stock: 10,
    isActive: true,
    attributes: [],
    rating: null,
    reviewCount: 0,
    ...overrides
  };
}
