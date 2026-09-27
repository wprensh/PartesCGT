import type { Product } from '@features/catalog';

export interface KitDefinition {
  id: string;
  code: string;
  goal: string;
  name: string;
  note: string;
  productIds: number[];
  /** Pregunta con la que se abre el asistente para confirmar compatibilidad. */
  question: string;
}

export interface Kit extends KitDefinition {
  products: Product[];
  total: number;
  /** Todas sus piezas están activas y con stock. */
  available: boolean;
}

/**
 * Cruza las definiciones con el catálogo en vivo (precio y stock actuales).
 * El catálogo público solo trae productos activos: si falta alguno, el kit no está disponible.
 */
export function resolveKits(definitions: KitDefinition[], catalog: Product[]): Kit[] {
  const byId = new Map(catalog.map(p => [p.id, p]));
  return definitions
    .map(def => {
      const products = def.productIds.map(id => byId.get(id)).filter((p): p is Product => !!p);
      return {
        ...def,
        products,
        total: products.reduce((sum, p) => sum + p.price, 0),
        available: products.length === def.productIds.length && products.every(p => p.stock > 0)
      };
    })
    .filter(kit => kit.products.length > 0);
}
