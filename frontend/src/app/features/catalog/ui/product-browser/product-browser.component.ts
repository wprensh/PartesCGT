import { ChangeDetectionStrategy, Component, ElementRef, computed, effect, inject, input, output, signal } from '@angular/core';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { RouterLink } from '@angular/router';
import { IconComponent } from '@shared/ui/icon/icon.component';
import { plural } from '@shared/utils/format';
import { Product } from '../../domain/product.model';
import { SORT_OPTIONS, SortKey } from '../../domain/sorting';
import { ProductBrowserStore } from '../../state/product-browser.store';
import { CatalogFiltersComponent } from '../catalog-filters/catalog-filters.component';
import { ProductCardComponent } from '../product-card/product-card.component';

/**
 * Catálogo navegable: búsqueda, orden, filtros y resultados.
 * No sabe nada del carrito ni del asistente: avisa con (add) y (askAssistant).
 */
@Component({
  selector: 'app-product-browser',
  imports: [CatalogFiltersComponent, IconComponent, MatPaginatorModule, ProductCardComponent, RouterLink],
  providers: [ProductBrowserStore],
  templateUrl: './product-browser.component.html',
  styleUrl: './product-browser.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '(document:keydown.escape)': 'filtersOpen.set(false)' }
})
export class ProductBrowserComponent {
  protected store = inject(ProductBrowserStore);
  private host = inject<ElementRef<HTMLElement>>(ElementRef);

  /** Slug de ?categoria= (null = todo el catálogo). */
  categorySlug = input<string | null | undefined>(null);
  add = output<Product>();
  askAssistant = output<string>();

  protected filtersOpen = signal(false);
  protected sortOptions = SORT_OPTIONS;
  protected resultsLabel = computed(() => plural(this.store.results().length, 'producto'));

  constructor() {
    effect(() => this.store.categorySlug.set(this.categorySlug() ?? null));
  }

  protected setQuery(event: Event) {
    this.store.query.set((event.target as HTMLInputElement).value);
  }

  protected setSort(event: Event) {
    this.store.sort.set((event.target as HTMLSelectElement).value as SortKey);
  }

  /** Cambia de página y sube al inicio del catálogo, para no quedar al final de la cuadrícula anterior. */
  protected changePage(event: PageEvent) {
    this.store.page.onPage(event);
    this.host.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }
}
