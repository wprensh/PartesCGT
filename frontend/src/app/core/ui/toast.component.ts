import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ToastService } from './toast.service';

@Component({
  selector: 'app-toast',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `@if (toast.message()) { <div class="toast" role="status">{{ toast.message() }}</div> }`,
  styles: `
    .toast { position: fixed; bottom: 1.25rem; left: 50%; transform: translateX(-50%); z-index: 30;
             background: #fff; color: var(--on-light); padding: .75rem 1.2rem; border-radius: 999px; font-weight: 500;
             box-shadow: 0 12px 40px rgba(0, 0, 0, .5); }
  `
})
export class ToastComponent {
  toast = inject(ToastService);
}
