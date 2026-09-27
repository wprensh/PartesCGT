export interface Supplier {
  id: number;
  name: string;
  /** NIT o documento. */
  taxId: string;
  contactName: string | null;
  email: string | null;
  phone: string | null;
  address: string | null;
  city: string | null;
  notes: string | null;
  isActive: boolean;
  productCount: number;
}

export type SupplierInput = Omit<Supplier, 'id' | 'productCount'>;

/** Un proveedor de un producto, con su costo de compra. */
export interface ProductSupplier {
  supplierId: number;
  supplierName: string;
  supplierIsActive: boolean;
  cost: number;
  supplierSku: string | null;
  isPreferred: boolean;
}

export type ProductSupplierInput = Pick<ProductSupplier, 'supplierId' | 'cost' | 'supplierSku' | 'isPreferred'>;

/** Un producto que surte un proveedor (vista desde el proveedor). */
export interface SuppliedProduct {
  productId: number;
  productName: string;
  price: number;
  cost: number;
  supplierSku: string | null;
  isPreferred: boolean;
}

/** Margen bruto sobre el precio de venta: (precio − costo) / precio. Null si no hay precio. */
export function grossMargin(price: number, cost: number): number | null {
  return price > 0 ? (price - cost) / price : null;
}
