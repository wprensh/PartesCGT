/**
 * Permisos del panel. Espejo de Domain/Access/Permissions.cs en el backend: si agregas uno allá, agrégalo aquí.
 * El backend es quien decide de verdad; el frontend solo los usa para mostrar u ocultar opciones.
 */
export const PERMISSIONS = {
  categoriesView: 'categories.view',
  categoriesManage: 'categories.manage',
  productsView: 'products.view',
  productsManage: 'products.manage',
  suppliersView: 'suppliers.view',
  suppliersManage: 'suppliers.manage',
  usersManage: 'users.manage'
} as const;

export type PermissionCode = (typeof PERMISSIONS)[keyof typeof PERMISSIONS];

/** Usuario conectado, tal como lo devuelve GET /api/auth/me. */
export interface Profile {
  id: number;
  email: string;
  fullName: string;
  role: string;
  permissions: string[];
}
