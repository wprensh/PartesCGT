import { Injectable, signal } from '@angular/core';

const DURATION_MS = 2200;

/** Avisos breves ("Agregado al carrito"). Uno a la vez: el nuevo reemplaza al anterior. */
@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly message = signal('');
  private timer?: ReturnType<typeof setTimeout>;

  show(message: string) {
    clearTimeout(this.timer);
    this.message.set(message);
    this.timer = setTimeout(() => this.message.set(''), DURATION_MS);
  }
}
