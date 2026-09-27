import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { CreateOrderRequest, Order } from '../domain/order.model';

@Injectable({ providedIn: 'root' })
export class OrdersApi {
  private http = inject(HttpClient);

  create(request: CreateOrderRequest) {
    return this.http.post<Order>('/api/orders', request);
  }
}
