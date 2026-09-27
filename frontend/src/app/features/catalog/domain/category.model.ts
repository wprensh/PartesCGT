export interface Category {
  id: number;
  name: string;
  /** null = categoría principal. */
  parentId: number | null;
  /** Incluye los productos de las subcategorías. */
  activeProducts: number;
  totalProducts: number;
}

/** Categoría lista para la tienda: con su slug de URL y su código corto. */
export interface StoreCategory extends Category {
  slug: string;
  code: string;
}
