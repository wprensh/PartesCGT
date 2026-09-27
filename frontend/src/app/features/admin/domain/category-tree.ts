import type { Category } from '@features/catalog';
import { compareText } from '@shared/utils/text';

/** Espacios duros: un <option> no respeta espacios normales al inicio. */
const SUBCATEGORY_INDENT = '\u00a0'.repeat(3);
const byName = (a: Category, b: Category) => compareText(a.name, b.name);

/** Solo las principales pueden tener subcategorías (un nivel). */
export function mainCategories(all: Category[]): Category[] {
  return all.filter(c => c.parentId === null).sort(byName);
}

/** Principales seguidas de sus subcategorías, en orden alfabético. */
export function categoryTree(all: Category[]): Category[] {
  return mainCategories(all).flatMap(parent => [parent, ...all.filter(c => c.parentId === parent.id).sort(byName)]);
}

/** Opciones para un <select>, con sangría en las subcategorías. */
export function categoryOptions(all: Category[]): { id: number; label: string }[] {
  return categoryTree(all).map(c => ({ id: c.id, label: c.parentId ? `${SUBCATEGORY_INDENT}↳ ${c.name}` : c.name }));
}

export function hasChildren(all: Category[], category: Category): boolean {
  return all.some(c => c.parentId === category.id);
}

export function describeCategory(all: Category[], c: Category): string {
  const subcategories = all.filter(x => x.parentId === c.id).length;
  const prefix = subcategories ? `${subcategories} subcategoría(s) · ` : '';
  if (c.totalProducts === 0) return `${prefix}Sin productos`;
  const hidden = c.totalProducts - c.activeProducts;
  return `${prefix}${c.totalProducts} producto(s)` + (hidden ? `, ${hidden} oculto(s)` : '');
}
