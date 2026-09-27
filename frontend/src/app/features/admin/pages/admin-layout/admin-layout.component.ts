import { ChangeDetectionStrategy, Component, computed, inject, viewChild } from '@angular/core';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '@core/auth/auth.service';
import { IconComponent } from '@shared/ui/icon/icon.component';
import { allowedSections } from '../../domain/admin-sections';
import { ChangePasswordDialogComponent } from '../../ui/change-password-dialog/change-password-dialog.component';

/** Marco del panel: barra (MatToolbar) con las secciones que el usuario puede ver, su identidad y su sesión. */
@Component({
  selector: 'app-admin-layout',
  imports: [ChangePasswordDialogComponent, IconComponent, MatToolbarModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './admin-layout.component.html',
  styleUrl: './admin-layout.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminLayoutComponent {
  protected auth = inject(AuthService);
  protected passwordDialog = viewChild.required(ChangePasswordDialogComponent);
  // can() lee la señal del perfil: el menú se recalcula solo cuando cambia.
  protected sections = computed(() => allowedSections(p => this.auth.can(p)));

  /** En pantallas angostas el menú se desplaza: la pestaña activa se trae a la vista (solo en horizontal). */
  protected revealTab(link: HTMLElement) {
    const nav = link.parentElement;
    if (!nav) return;
    nav.scrollTo({ left: link.offsetLeft - (nav.clientWidth - link.offsetWidth) / 2 });
  }
}
