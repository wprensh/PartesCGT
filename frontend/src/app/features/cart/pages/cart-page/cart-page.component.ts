import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { describeHttpError } from '@core/http/http-error';
import { CopPipe } from '@shared/pipes/cop.pipe';
import { IconComponent } from '@shared/ui/icon/icon.component';
import { CartStore } from '../../data/cart.store';
import { OrdersApi } from '../../data/orders.api';
import { Order } from '../../domain/order.model';

@Component({
  selector: 'app-cart-page',
  imports: [CopPipe, IconComponent, ReactiveFormsModule, RouterLink],
  templateUrl: './cart-page.component.html',
  styleUrl: './cart-page.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CartPageComponent {
  protected cart = inject(CartStore);
  private orders = inject(OrdersApi);

  protected form = inject(NonNullableFormBuilder).group({
    customerName: ['', [Validators.required, Validators.maxLength(120)]],
    customerEmail: ['', [Validators.required, Validators.email]]
  });

  protected sending = signal(false);
  protected error = signal('');
  protected order = signal<Order | null>(null);

  protected checkout() {
    if (this.form.invalid) return;
    this.sending.set(true);
    this.error.set('');
    const items = this.cart.items().map(i => ({ productId: i.product.id, quantity: i.quantity }));

    this.orders.create({ ...this.form.getRawValue(), items }).subscribe({
      next: order => {
        this.order.set(order);
        this.cart.clear();
        this.sending.set(false);
      },
      error: (e: HttpErrorResponse) => {
        this.error.set(describeHttpError(e));
        this.sending.set(false);
      }
    });
  }
}
