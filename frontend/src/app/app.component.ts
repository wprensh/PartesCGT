import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ViewportScroller } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { ToastComponent } from '@core/ui/toast.component';
import { AssistantChatComponent } from '@features/assistant';
import { SiteFooterComponent } from './layout/site-footer/site-footer.component';
import { SiteHeaderComponent } from './layout/site-header/site-header.component';

/** Alto de la barra sticky (60px) + margen, para que los saltos a #kits o #catalogo no queden tapados. */
const ANCHOR_OFFSET_PX = 76;

@Component({
  selector: 'app-root',
  imports: [AssistantChatComponent, RouterOutlet, SiteFooterComponent, SiteHeaderComponent, ToastComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-site-header [adminMode]="inAdmin()" />
    <main class="container"><router-outlet /></main>
    @if (!inAdmin()) {
      <app-site-footer />
      <app-assistant-chat />
    }
    <app-toast />
  `,
  styles: `main { display: block; padding-top: 1.75rem; padding-bottom: 4rem; }`
})
export class AppComponent {
  protected inAdmin = toSignal(
    inject(Router).events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map(e => e.urlAfterRedirects.startsWith('/admin'))
    ),
    { initialValue: false }
  );

  constructor() {
    inject(ViewportScroller).setOffset([0, ANCHOR_OFFSET_PX]);
  }
}
