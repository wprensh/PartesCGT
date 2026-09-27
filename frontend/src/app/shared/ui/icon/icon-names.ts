/**
 * Iconos disponibles. Google Fonts solo descarga los que aparecen en `icon_names` de src/index.html.
 * Para usar uno nuevo: agrégalo aquí Y en index.html (en orden alfabético). El tipo IconName hace que
 * TypeScript marque como error cualquier icono que no esté en esta lista.
 */
export const ICON_NAMES = [
  'add', 'add_shopping_cart', 'admin_panel_settings', 'arrow_forward', 'category', 'chevron_left', 'close',
  'expand_more', 'group', 'inventory_2', 'key', 'local_shipping', 'logout', 'menu', 'remove', 'search',
  'shopping_cart', 'smart_toy', 'star', 'storefront', 'tune', 'upload', 'verified', 'visibility'
] as const;

export type IconName = (typeof ICON_NAMES)[number];
