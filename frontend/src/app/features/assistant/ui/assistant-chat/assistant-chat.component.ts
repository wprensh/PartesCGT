import {
  ChangeDetectionStrategy, Component, ElementRef, afterRenderEffect, effect, inject, signal, untracked, viewChild
} from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { describeHttpError } from '@core/http/http-error';
import { ToastService } from '@core/ui/toast.service';
import { CartStore } from '@features/cart';
import { Product, ProductCardComponent } from '@features/catalog';
import { IconComponent } from '@shared/ui/icon/icon.component';
import { AssistantApi } from '../../data/assistant.api';
import { AssistantLauncher } from '../../data/assistant-launcher.service';
import { CHAT_CONTEXT_SIZE, ChatMessage } from '../../domain/chat.model';

/** Chat flotante con el asistente de compras. */
@Component({
  selector: 'app-assistant-chat',
  imports: [FormsModule, IconComponent, ProductCardComponent],
  templateUrl: './assistant-chat.component.html',
  styleUrl: './assistant-chat.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AssistantChatComponent {
  private api = inject(AssistantApi);
  private cart = inject(CartStore);
  private toast = inject(ToastService);
  private launcher = inject(AssistantLauncher);
  private log = viewChild<ElementRef<HTMLElement>>('log');

  protected open = this.launcher.open;
  protected messages = signal<ChatMessage[]>([]);
  protected thinking = signal(false);
  protected error = signal('');
  protected draft = signal('');

  constructor() {
    // Mantener el scroll al final cuando llegan mensajes.
    afterRenderEffect(() => {
      this.messages();
      const el = this.log()?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    });

    // Pregunta sugerida desde otra parte: se deja en el borrador para que la persona la complete y la envíe.
    effect(() => {
      const question = this.launcher.suggestion();
      if (!question) return;
      untracked(() => {
        this.draft.set(question);
        this.launcher.suggestion.set('');
      });
    });
  }

  protected send() {
    const text = this.draft().trim();
    if (!text || this.thinking()) return;

    this.draft.set('');
    this.error.set('');
    this.messages.update(m => [...m, { role: 'user', content: text }]);
    this.thinking.set(true);

    this.api.chat(this.messages().slice(-CHAT_CONTEXT_SIZE)).subscribe({
      next: reply => {
        this.messages.update(m => [...m, { role: 'assistant', content: reply.answer, products: reply.products }]);
        this.thinking.set(false);
      },
      error: (e: HttpErrorResponse) => {
        this.error.set(e.status === 429
          ? 'Demasiadas preguntas seguidas. Espera un minuto e intenta de nuevo.'
          : describeHttpError(e));
        // Se quita el mensaje sin respuesta para no romper la alternancia usuario/asistente.
        this.messages.update(m => m.slice(0, -1));
        this.draft.set(text);
        this.thinking.set(false);
      }
    });
  }

  protected addToCart(product: Product) {
    this.cart.add(product);
    this.toast.show(`${product.name} agregado al carrito`);
  }
}
