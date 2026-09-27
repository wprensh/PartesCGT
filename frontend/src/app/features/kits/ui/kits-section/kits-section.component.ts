import { ChangeDetectionStrategy, Component, inject, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, map, of } from 'rxjs';
import { CatalogApi } from '@features/catalog';
import { CopPipe } from '@shared/pipes/cop.pipe';
import { IconComponent } from '@shared/ui/icon/icon.component';
import { KIT_DEFINITIONS } from '../../domain/kit-definitions';
import { Kit, resolveKits } from '../../domain/kit.model';

/** "¿Qué quieres lograr?": kits por objetivo. Avisa con (addKit) y (ask); no conoce carrito ni asistente. */
@Component({
  selector: 'app-kits-section',
  imports: [CopPipe, IconComponent],
  templateUrl: './kits-section.component.html',
  styleUrl: './kits-section.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class KitsSectionComponent {
  addKit = output<Kit>();
  ask = output<string>();

  // Los kits se arman con el catálogo completo, sin filtros.
  protected kits = toSignal(
    inject(CatalogApi).products().pipe(
      map(catalog => resolveKits(KIT_DEFINITIONS, catalog)),
      catchError(() => of([] as Kit[]))
    ),
    { initialValue: [] as Kit[] }
  );
}
