import { ChangeDetectionStrategy, Component, computed, inject, input, model, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CopPipe } from '@shared/pipes/cop.pipe';
import { IconComponent } from '@shared/ui/icon/icon.component';
import { plural } from '@shared/utils/format';
import { CategoryStore } from '../../data/category.store';
import { StoreCategory } from '../../domain/category.model';
import { Facets } from '../../domain/facets';
import {
  EMPTY_FILTERS, FilterState, activeFilterCount, isAttributeSelected, toggleAttribute, toggleBrand
} from '../../domain/filter-state';

type PriceKey = 'minPrice' | 'maxPrice';

/** Panel de filtros. El estado entra y sale por [(filters)]; en celular el contenedor lo muestra como cajón. */
@Component({
  selector: 'app-catalog-filters',
  imports: [CopPipe, IconComponent, RouterLink],
  templateUrl: './catalog-filters.component.html',
  styleUrl: './catalog-filters.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CatalogFiltersComponent {
  protected categories = inject(CategoryStore);

  facets = input.required<Facets>();
  category = input<StoreCategory | null>(null);
  resultCount = input(0);
  filters = model.required<FilterState>();
  closed = output<void>();

  protected activeCount = computed(() => activeFilterCount(this.filters()));
  protected resultsLabel = computed(() => `Ver ${plural(this.resultCount(), 'producto')}`);

  /** Categoría principal que se está viendo (la propia o la de la subcategoría). */
  protected parent = computed(() => {
    const c = this.category();
    return c?.parentId ? this.categories.byId(c.parentId) : c;
  });

  protected isAttributeSelected = (name: string, value?: string) => isAttributeSelected(this.filters(), name, value);

  protected clear() {
    this.filters.set(EMPTY_FILTERS);
  }

  protected patch(changes: Partial<FilterState>) {
    this.filters.update(f => ({ ...f, ...changes }));
  }

  protected setPrice(key: PriceKey, event: Event) {
    const raw = (event.target as HTMLInputElement).value;
    const value = raw === '' ? NaN : Math.max(0, Math.round(Number(raw)));
    this.patch({ [key]: Number.isFinite(value) ? value : null });
  }

  protected toggleBrand(brand: string) {
    this.filters.update(f => toggleBrand(f, brand));
  }

  protected toggleAttribute(name: string, value: string) {
    this.filters.update(f => toggleAttribute(f, name, value));
  }
}
