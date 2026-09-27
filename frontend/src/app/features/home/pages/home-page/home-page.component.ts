import { ChangeDetectionStrategy, Component, ElementRef, inject, input, viewChild } from '@angular/core';
import { ToastService } from '@core/ui/toast.service';
import { AssistantLauncher } from '@features/assistant';
import { CartStore } from '@features/cart';
import { Product, ProductBrowserComponent } from '@features/catalog';
import { Kit, KitsSectionComponent } from '@features/kits';
import { StoreHeroComponent } from '../../ui/store-hero/store-hero.component';

/**
 * Página de inicio. Es el único lugar que conecta catálogo y kits con el carrito y el asistente:
 * las secciones solo emiten eventos y esta página decide qué hacer.
 */
@Component({
  selector: 'app-home-page',
  imports: [KitsSectionComponent, ProductBrowserComponent, StoreHeroComponent],
  templateUrl: './home-page.component.html',
  styleUrl: './home-page.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HomePageComponent {
  private cart = inject(CartStore);
  private toast = inject(ToastService);
  protected assistant = inject(AssistantLauncher);
  private kitsSection = viewChild.required<ElementRef<HTMLElement>>('kitsSection');

  /** ?categoria=memoria (enlazado por withComponentInputBinding). */
  categoria = input<string>();

  protected scrollToKits() {
    this.kitsSection().nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  protected addProduct(product: Product) {
    this.cart.add(product);
    this.toast.show(`${product.name} agregado al carrito`);
  }

  protected addKit(kit: Kit) {
    this.cart.addMany(kit.products);
    this.toast.show(`${kit.name} agregado al carrito (${kit.products.length} piezas)`);
  }
}
