import type { Product } from '@features/catalog';

/** Lo que el panel envía al crear o editar un producto. */
export type ProductInput = Pick<Product,
  'name' | 'description' | 'brand' | 'categoryId' | 'price' | 'stock' | 'isActive' | 'attributes' | 'imageUrl'>;

/** Mismo límite que Product.ImageUrl en la base de datos. */
export const MAX_IMAGE_URL_LENGTH = 500;

export const MAX_ATTRIBUTES = 20;
