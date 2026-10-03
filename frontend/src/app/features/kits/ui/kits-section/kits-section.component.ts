import { ChangeDetectionStrategy, Component, inject, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatPaginatorModule } from '@angular/material/paginator';
import { catchError, map, of } from 'rxjs';
import { CatalogApi } from '@features/catalog';
import { CopPipe } from '@shared/pipes/cop.pipe';
import { paginatedList } from '@shared/table/paginated-list';
import { IconComponent } from '@shared/ui/icon/icon.component';
import { KIT_DEFINITIONS } from '../../domain/kit-definitions';
import { Kit, resolveKits } from '../../domain/kit.model';

/** Kits por página: una fila de 3 en escritorio. */
export const KITS_PAGE_SIZE = 3;

/**
 * "¿Qué quieres lograr?": kits por objetivo, de a 3 por página.
 * Avisa con (addKit), (ask) y (viewMore); no conoce carrito, asistente ni la futura página de kits.
 */
@Component({
  selector: 'app-kits-section',
  imports: [CopPipe, IconComponent, MatPaginatorModule],
  templateUrl: './kits-section.component.html',
  styleUrl: './kits-section.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class KitsSectionComponent {
  addKit = output<Kit>();
  ask = output<string>();
  /** "Ver más kits". Todavía no tiene acción: quien use la sección decidirá qué hacer (p. ej. ir a /kits). */
  viewMore = output<void>();

  // Los kits se arman con el catálogo completo, sin filtros.
  protected kits = toSignal(
    inject(CatalogApi).products().pipe(
      map(catalog => resolveKits(KIT_DEFINITIONS, catalog)),
      catchError(() => of([] as Kit[]))
    ),
    { initialValue: [] as Kit[] }
  );

  protected readonly page = paginatedList(this.kits, KITS_PAGE_SIZE);
}
