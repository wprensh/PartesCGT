import { Routes } from '@angular/router';

export const CART_ROUTES: Routes = [
  {
    path: '',
    title: 'Carrito',
    loadComponent: () => import('./pages/cart-page/cart-page.component').then(m => m.CartPageComponent)
  }
];
