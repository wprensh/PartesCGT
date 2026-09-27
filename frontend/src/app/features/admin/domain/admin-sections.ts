import { PERMISSIONS, PermissionCode } from '@core/auth/permissions';
import type { IconName } from '@shared/ui/icon/icon-names';

export interface AdminSection {
  path: string;
  label: string;
  /** Permiso mínimo para ver la sección. */
  permission: PermissionCode;
  icon: IconName;
}

/** Secciones del panel, en el orden del menú. Cada una se muestra solo a quien tiene su permiso. */
export const ADMIN_SECTIONS: readonly AdminSection[] = [
  { path: 'productos', label: 'Productos', permission: PERMISSIONS.productsView, icon: 'inventory_2' },
  { path: 'categorias', label: 'Categorías', permission: PERMISSIONS.categoriesView, icon: 'category' },
  { path: 'proveedores', label: 'Proveedores', permission: PERMISSIONS.suppliersView, icon: 'local_shipping' },
  { path: 'usuarios', label: 'Usuarios', permission: PERMISSIONS.usersManage, icon: 'group' },
  { path: 'roles', label: 'Roles', permission: PERMISSIONS.usersManage, icon: 'admin_panel_settings' }
];

export function allowedSections(can: (p: PermissionCode) => boolean): AdminSection[] {
  return ADMIN_SECTIONS.filter(s => can(s.permission));
}
