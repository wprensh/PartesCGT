/**
 * API pública de la feature catalog. Las demás features importan solo desde aquí,
 * nunca desde sus carpetas internas.
 */
export type { Product, ProductAttribute } from './domain/product.model';
export { LOW_STOCK } from './domain/product.model';
export { MAX_IMAGE_BYTES, ACCEPTED_IMAGE_TYPES, safeImageUrl, validateImageFile } from './domain/product-image';
export type { Category, StoreCategory } from './domain/category.model';
export { CatalogApi, toParams } from './data/catalog.api';
export type { ProductQuery } from './data/catalog.api';
export { CategoryStore } from './data/category.store';
export { ProductCardComponent } from './ui/product-card/product-card.component';
export { ProductDetailDialogComponent } from './ui/product-detail-dialog/product-detail-dialog.component';
export type { ProductDetailData, ProductDetailResult } from './ui/product-detail-dialog/product-detail-dialog.component';
export { ProductDetailDialog } from './ui/product-detail-dialog/product-detail-dialog.service';
export { ProductBrowserComponent } from './ui/product-browser/product-browser.component';
export { CategoryMenuComponent } from './ui/category-menu/category-menu.component';
