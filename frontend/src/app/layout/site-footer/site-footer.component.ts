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
          <a routerLink="/" class="brand">Partes<span>CTG</span></a>
          <p>Repuestos y mejoras para computador en Cartagena.</p>
        </div>
        <nav aria-label="Pie de página">
          <a routerLink="/">Catálogo</a>
          <a routerLink="/carrito">Carrito</a>
        </nav>
        <small>© {{ year }} Partes CTG</small>
      </div>
    </footer>
  `,
  styles: `
    .site-foot { background: var(--deep); color: var(--muted); border-top: 1px solid var(--line); }
    /* Espacio abajo para que el botón flotante del asistente no tape el pie. */
    .foot { display: flex; flex-wrap: wrap; gap: 1.5rem; justify-content: space-between; align-items: flex-start;
            padding-top: 2rem; padding-bottom: 5.5rem; }
    .brand { font: 700 1.4rem var(--display); color: #fff; text-decoration: none; letter-spacing: .02em; }
    .brand span { color: var(--muted); margin-left: .15rem; }
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
