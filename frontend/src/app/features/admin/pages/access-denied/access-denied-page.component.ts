import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '@core/auth/auth.service';
import { allowedSections } from '../../domain/admin-sections';

@Component({
  selector: 'app-access-denied-page',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="card denied">
      <h1>No tienes acceso a esta sección</h1>
      <p>Tu rol ({{ auth.profile()?.role }}) no incluye ese permiso. Si lo necesitas, pídeselo a un administrador.</p>
      @if (firstAllowed(); as section) {
        <a class="btn" [routerLink]="['/admin', section.path]">Ir a {{ section.label }}</a>
      } @else {
        <p>Tu rol todavía no tiene ningún permiso del panel.</p>
      }
    </section>
  `,
  styles: `
    .denied { max-width: 34rem; margin: 2rem auto; padding: 1.75rem; display: grid; gap: .75rem; justify-items: start; }
    h1 { font-size: 1.5rem; }
    p { margin: 0; color: var(--muted); }
    a { text-decoration: none; }
  `
})
export class AccessDeniedPageComponent {
  protected auth = inject(AuthService);
  protected firstAllowed = computed(() => allowedSections(p => this.auth.can(p))[0] ?? null);
}
