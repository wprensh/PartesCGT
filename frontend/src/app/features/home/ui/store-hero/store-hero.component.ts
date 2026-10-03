import { ChangeDetectionStrategy, Component, DestroyRef, afterNextRender, inject, input, output } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { IconComponent } from '@shared/ui/icon/icon.component';
import { IconName } from '@shared/ui/icon/icon-names';

interface TrustPoint { icon: IconName; text: string; }

/**
 * Foto del banner, servida desde public/ (se copia tal cual al sitio publicado).
 * 1920 px de ancho y ~300 KB: para cambiarla, deja la nueva en public/images/ y actualiza esta ruta.
 */
export const DEFAULT_HERO_IMAGE = '/images/hero-motherboard.jpg';

/** Portada de la tienda: banner de extremo a extremo con foto de fondo. Presentacional. */
@Component({
  selector: 'app-store-hero',
  imports: [IconComponent],
  templateUrl: './store-hero.component.html',
  styleUrl: './store-hero.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StoreHeroComponent {
  /** Foto del banner. 2560×1000, motivo principal en el 40 % derecho. */
  imageUrl = input<string>(DEFAULT_HERO_IMAGE);
  /** Crédito de la foto. Vacío = no se muestra (fotos propias). */
  imageCredit = input<string>('');
  imageCreditUrl = input<string>('');

  viewKits = output<void>();
  askAssistant = output<void>();

  protected trustPoints: TrustPoint[] = [
    { icon: 'local_shipping', text: 'Envío el mismo día en Cartagena' },
    { icon: 'verified', text: 'Kits con compatibilidad revisada' },
    { icon: 'smart_toy', text: 'Asistente que conoce el inventario' }
  ];

  constructor() {
    // 100vw incluye la barra de scroll: publicamos su ancho en --sbw para que el banner
    // mida exactamente el área visible y el texto quede alineado con .container.
    const doc = inject(DOCUMENT);
    const destroyRef = inject(DestroyRef);
    afterNextRender(() => {
      const root = doc.documentElement;
      const win = doc.defaultView;
      if (!win) return;
      const update = () => root.style.setProperty('--sbw', `${win.innerWidth - root.clientWidth}px`);
      const observer = new ResizeObserver(update);
      observer.observe(doc.body);
      update();
      destroyRef.onDestroy(() => observer.disconnect());
    });
  }
}
