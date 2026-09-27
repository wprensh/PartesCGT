import {
  ChangeDetectionStrategy, Component, ElementRef, Injector, afterNextRender, inject, output, signal, viewChild
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { IsActiveMatchOptions, NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { filter } from 'rxjs';
import { IconComponent } from '@shared/ui/icon/icon.component';
import { CategoryStore } from '../../data/category.store';

/** Menú "Categorías" de la barra superior: desplegable en escritorio, panel lateral en celular. */
@Component({
  selector: 'app-category-menu',
  imports: [IconComponent, RouterLink, RouterLinkActive],
  templateUrl: './category-menu.component.html',
  styleUrl: './category-menu.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '(document:keydown.escape)': 'close(true)',
    '(document:click)': 'closeIfOutside($event)'
  }
})
export class CategoryMenuComponent {
  protected categories = inject(CategoryStore);
  private host = inject<ElementRef<HTMLElement>>(ElementRef);
  private injector = inject(Injector);
  private trigger = viewChild.required<ElementRef<HTMLButtonElement>>('trigger');
  private panel = viewChild<ElementRef<HTMLElement>>('panel');

  /** El menú no conoce al asistente: solo avisa y el contenedor decide. */
  askAssistant = output<void>();

  protected open = signal(false);
  // Resalta la categoría activa comparando ?categoria=, sin importar el #fragmento.
  protected activeOptions: IsActiveMatchOptions = { paths: 'exact', queryParams: 'exact', fragment: 'ignored', matrixParams: 'ignored' };

  constructor() {
    inject(Router).events.pipe(filter(e => e instanceof NavigationEnd), takeUntilDestroyed())
      .subscribe(() => this.open.set(false));
  }

  protected toggle() {
    if (this.open()) return this.close();
    this.open.set(true);
    afterNextRender(() => this.panel()?.nativeElement.querySelector<HTMLElement>('.inner a')?.focus(),
      { injector: this.injector });
  }

  close(returnFocus = false) {
    if (!this.open()) return;
    this.open.set(false);
    if (returnFocus) this.trigger().nativeElement.focus();
  }

  protected ask() {
    this.close();
    this.askAssistant.emit();
  }

  /** Cierra al elegir un enlace, aunque la URL no cambie (misma categoría). */
  protected closeOnLink(event: MouseEvent) {
    if ((event.target as HTMLElement).closest('a')) this.close();
  }

  closeIfOutside(event: MouseEvent) {
    if (!this.host.nativeElement.contains(event.target as Node)) this.close();
  }
}
