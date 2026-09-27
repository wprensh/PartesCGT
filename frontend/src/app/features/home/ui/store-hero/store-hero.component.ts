import { ChangeDetectionStrategy, Component, output } from '@angular/core';
import { IconComponent } from '@shared/ui/icon/icon.component';
import { IconName } from '@shared/ui/icon/icon-names';

interface TrustPoint { icon: IconName; text: string; }

/** Portada de la tienda. Presentacional. */
@Component({
  selector: 'app-store-hero',
  imports: [IconComponent],
  templateUrl: './store-hero.component.html',
  styleUrl: './store-hero.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StoreHeroComponent {
  viewKits = output<void>();
  askAssistant = output<void>();

  protected trustPoints: TrustPoint[] = [
    { icon: 'local_shipping', text: 'Envío el mismo día en Cartagena' },
    { icon: 'verified', text: 'Kits con compatibilidad revisada' },
    { icon: 'smart_toy', text: 'Asistente que conoce el inventario' }
  ];
}
