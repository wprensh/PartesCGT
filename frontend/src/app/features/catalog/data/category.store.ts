import { Injectable, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, of } from 'rxjs';
import { compareText, slugify } from '@shared/utils/text';
import { Category, StoreCategory } from '../domain/category.model';
import { categoryCode, groupByFamily } from '../domain/category-families';
import { CatalogApi } from './catalog.api';

/** Categorías de la tienda, compartidas por el menú superior, el catálogo y los filtros. */
@Injectable({ providedIn: 'root' })
export class CategoryStore {
  private api = inject(CatalogApi);

  // undefined = cargando; así quien filtra por ?categoria= puede esperar a conocerlas.
  private raw = toSignal(this.api.categories().pipe(catchError(() => of([] as Category[]))));

  readonly loaded = computed(() => this.raw() !== undefined);

  /** Todas las categorías con productos activos (principales y subcategorías). */
  private all = computed(() => {
    const visible = (this.raw() ?? []).filter(c => c.activeProducts > 0);
    const byId = new Map(visible.map(c => [c.id, c]));
    return visible.map((c): StoreCategory => ({
      ...c,
      slug: slugify(c.name),
      // Una subcategoría usa el código de su padre (SSD NVMe → SSD).
      code: categoryCode(c.parentId ? byId.get(c.parentId)?.name ?? c.name : c.name)
    }));
  });

  /** Categorías principales agrupadas por familia, para el menú. */
  readonly families = computed(() => groupByFamily(this.all().filter(c => c.parentId === null)));

  /** Principales en el mismo orden que el menú. */
  readonly mainCategories = computed(() => this.families().flatMap(f => f.items));

  bySlug(slug: string | null | undefined): StoreCategory | null {
    return slug ? this.all().find(c => c.slug === slug) ?? null : null;
  }

  byId(id: number | null | undefined): StoreCategory | null {
    return id ? this.all().find(c => c.id === id) ?? null : null;
  }

  childrenOf(parentId: number): StoreCategory[] {
    return this.all().filter(c => c.parentId === parentId).sort((a, b) => compareText(a.name, b.name));
  }
}
