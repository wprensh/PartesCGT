import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-site-footer',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <footer class="site-foot">
      <div class="container foot">
        <div>
          <a routerLink="/" class="brand" aria-label="Prens Tecnología, inicio">
            <span class="brand-word">prens<i aria-hidden="true"></i></span>
            <span class="brand-sub" aria-hidden="true">TECNOLOGÍA</span>
          </a>
          <p>Repuestos y mejoras para computador en Cartagena.<br>Una empresa de los hermanos Prens.</p>
        </div>
        <nav aria-label="Pie de página">
          <a routerLink="/">Catálogo</a>
          <a routerLink="/carrito">Carrito</a>
        </nav>
        <small>© {{ year }} Prens Tecnología</small>
      </div>
    </footer>
  `,
  styles: `
    .site-foot { background: var(--deep); color: var(--muted); border-top: 1px solid var(--line); }
    /* Espacio abajo para que el botón flotante del asistente no tape el pie. */
    .foot { display: flex; flex-wrap: wrap; gap: 1.5rem; justify-content: space-between; align-items: flex-start;
            padding-top: 2rem; padding-bottom: 5.5rem; }
    .brand { display: inline-flex; flex-direction: column; gap: 4px; line-height: 1; color: #fff; text-decoration: none; }
    .brand-word { display: flex; align-items: flex-end; gap: 3px; font: 800 28px/.8 var(--display); letter-spacing: -.06em; }
    .brand-word i { width: 7px; height: 7px; border-radius: 50%; background: var(--gold); margin-bottom: 1px; }
    .brand-sub { font: 500 8px var(--mono); letter-spacing: .3em; color: var(--muted); }
    p { margin: .35rem 0 0; font-size: .9rem; }
    nav { display: flex; gap: 1.25rem; }
    nav a { color: #fff; text-decoration: none; font-weight: 600; }
    nav a:hover { text-decoration: underline; }
    small { align-self: flex-end; }
  `
})
export class SiteFooterComponent {
  protected readonly year = new Date().getFullYear();
}
