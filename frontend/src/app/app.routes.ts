import { Routes } from '@angular/router';

/** Cada feature declara sus propias rutas y se carga bajo demanda. */
export const routes: Routes = [
  { path: '', loadChildren: () => import('./features/home/home.routes').then(m => m.HOME_ROUTES) },
  { path: 'carrito', loadChildren: () => import('./features/cart/cart.routes').then(m => m.CART_ROUTES) },
  { path: 'admin', loadChildren: () => import('./features/admin/admin.routes').then(m => m.ADMIN_ROUTES) },
  { path: '**', redirectTo: '' }
];
