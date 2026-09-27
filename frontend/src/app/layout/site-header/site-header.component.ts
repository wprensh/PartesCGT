import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AssistantLauncher } from '@features/assistant';
import { CartStore } from '@features/cart';
import { CategoryMenuComponent } from '@features/catalog';
import { IconComponent } from '@shared/ui/icon/icon.component';

@Component({
  selector: 'app-site-header',
  imports: [CategoryMenuComponent, IconComponent, RouterLink],
  templateUrl: './site-header.component.html',
  styleUrl: './site-header.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SiteHeaderComponent {
  protected cart = inject(CartStore);
  protected assistant = inject(AssistantLauncher);

  /** En el panel de administración se oculta la navegación de la tienda. */
  adminMode = input(false);
}
