import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, LOCALE_ID, computed, effect, inject, signal, untracked } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { catchError, combineLatest, debounceTime, distinctUntilChanged, filter, map, of, startWith, switchMap } from 'rxjs';
import { describeHttpError } from '@core/http/http-error';
import { paginatedList } from '@shared/table/paginated-list';
import { formatCop } from '@shared/utils/format';
import { CatalogApi } from '../data/catalog.api';
import { CategoryStore } from '../data/category.store';
import { applyFilters, buildFacets } from '../domain/facets';
import { ActiveFilter, EMPTY_FILTERS, FilterState, activeFilterCount, describeActiveFilters } from '../domain/filter-state';
import { Product } from '../domain/product.model';
import { SortKey, sortProducts } from '../domain/sorting';

const SEARCH_DEBOUNCE_MS = 250;
/** Tarjetas por página: una cuadrícula de 3 × 3 en escritorio. */
export const CATALOG_PAGE_SIZE = 9;

type LoadState =
  | { status: 'loading' }
  | { status: 'ok'; products: Product[] }
  | { status: 'error'; detail: string };

/**
 * Estado del buscador de productos: categoría (de la URL), búsqueda, filtros y orden.
 * Se provee en el componente ProductBrowser, así cada instancia tiene su propio estado.
 */
@Injectable()
export class ProductBrowserStore {
  private api = inject(CatalogApi);
  private categories = inject(CategoryStore);
  private locale = inject(LOCALE_ID);

  // ---------- Entradas ----------
  readonly categorySlug = signal<string | null>(null);
  readonly query = signal('');
  readonly sort = signal<SortKey>('relevance');
  readonly filters = signal<FilterState>(EMPTY_FILTERS);

  // ---------- Categoría ----------
  readonly category = computed(() => this.categories.bySlug(this.categorySlug()));
  private readonly categoryId = computed(() => this.category()?.id ?? null);
  readonly parentCategory = computed(() => {
    const c = this.category();
    return c?.parentId ? this.categories.byId(c.parentId) : null;
  });

  // ---------- Carga (el servidor filtra por categoría y búsqueda) ----------
  private readonly serverQuery = computed(() => ({
    // Con ?categoria= se espera a tener las categorías, para no cargar primero todo el catálogo.
    ready: !this.categorySlug() || this.categories.loaded(),
    categoryId: this.categoryId()
  }));

  private readonly load = toSignal(
    combineLatest([
      toObservable(this.query).pipe(debounceTime(SEARCH_DEBOUNCE_MS)),
      toObservable(this.serverQuery).pipe(filter(s => s.ready), map(s => s.categoryId), distinctUntilChanged())
    ]).pipe(
      switchMap(([q, categoryId]) => this.api.products({ q, categoryId }).pipe(
        map((products): LoadState => ({ status: 'ok', products })),
        startWith<LoadState>({ status: 'loading' }),
        catchError((e: HttpErrorResponse) => of<LoadState>({ status: 'error', detail: describeHttpError(e) }))
      ))
    ),
    { initialValue: { status: 'loading' } as LoadState }
  );

  readonly status = computed(() => this.load().status);
  readonly error = computed(() => { const l = this.load(); return l.status === 'error' ? l.detail : ''; });
  /** Productos de la categoría/búsqueda, antes de los filtros del panel. */
  readonly scope = computed(() => { const l = this.load(); return l.status === 'ok' ? l.products : []; });

  // ---------- Filtros (en el navegador) ----------
  readonly facets = computed(() => buildFacets(this.scope(), this.filters()));
  readonly results = computed(() => sortProducts(applyFilters(this.scope(), this.filters()), this.sort()));
  readonly activeCount = computed(() => activeFilterCount(this.filters()));
  readonly activeFilters = computed(() => describeActiveFilters(this.filters(), n => formatCop(n, this.locale)));

  // ---------- Paginación (en el navegador) ----------
  readonly page = paginatedList(this.results, CATALOG_PAGE_SIZE);

  constructor() {
    // Al cambiar de categoría, las marcas y atributos marcados dejan de tener sentido.
    effect(() => {
      this.categoryId();
      untracked(() => this.clearFilters());
    });
    // Otra búsqueda, filtro, orden o categoría = otros resultados: se vuelve a la primera página.
    effect(() => {
      this.query();
      this.filters();
      this.sort();
      this.categoryId();
      untracked(() => this.page.reset());
    });
  }

  removeFilter(chip: ActiveFilter) {
    this.filters.update(chip.remove);
  }

  clearFilters() {
    this.filters.set(EMPTY_FILTERS);
  }
}
