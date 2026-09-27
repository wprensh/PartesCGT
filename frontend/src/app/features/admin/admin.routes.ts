import { inject } from '@angular/core';
import { CanActivateFn, Router, Routes } from '@angular/router';
import { adminGuard, requirePermission } from '@core/auth/auth.guard';
import { AuthService } from '@core/auth/auth.service';
import { PERMISSIONS } from '@core/auth/permissions';
import { allowedSections } from './domain/admin-sections';

/** /admin lleva a la primera sección que el usuario puede ver (o a "sin acceso" si no tiene ninguna). */
const toFirstAllowedSection: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);   // antes del await: después ya no hay contexto de inyección
  await auth.ensureProfile();
  const first = allowedSections(p => auth.can(p))[0];
  return router.createUrlTree(['/admin', first?.path ?? 'sin-acceso']);
};

export const ADMIN_ROUTES: Routes = [
  {
    path: 'login',
    title: 'Ingresar al panel',
    loadComponent: () => import('./pages/login/login-page.component').then(m => m.LoginPageComponent)
  },
  {
    path: '',
    canActivate: [adminGuard],
    loadComponent: () => import('./pages/admin-layout/admin-layout.component').then(m => m.AdminLayoutComponent),
    children: [
      { path: '', pathMatch: 'full', canActivate: [toFirstAllowedSection], children: [] },
      {
        path: 'productos',
        title: 'Productos · Panel',
        canActivate: [requirePermission(PERMISSIONS.productsView)],
        loadComponent: () => import('./pages/products-admin/products-admin-page.component').then(m => m.ProductsAdminPageComponent)
      },
      {
        path: 'categorias',
        title: 'Categorías · Panel',
        canActivate: [requirePermission(PERMISSIONS.categoriesView)],
        loadComponent: () => import('./pages/categories-admin/categories-admin-page.component').then(m => m.CategoriesAdminPageComponent)
      },
      {
        path: 'proveedores',
        title: 'Proveedores · Panel',
        canActivate: [requirePermission(PERMISSIONS.suppliersView)],
        loadComponent: () => import('./pages/suppliers-admin/suppliers-admin-page.component').then(m => m.SuppliersAdminPageComponent)
      },
      {
        path: 'usuarios',
        title: 'Usuarios · Panel',
        canActivate: [requirePermission(PERMISSIONS.usersManage)],
        loadComponent: () => import('./pages/users-admin/users-admin-page.component').then(m => m.UsersAdminPageComponent)
      },
      {
        path: 'roles',
        title: 'Roles · Panel',
        canActivate: [requirePermission(PERMISSIONS.usersManage)],
        loadComponent: () => import('./pages/roles-admin/roles-admin-page.component').then(m => m.RolesAdminPageComponent)
      },
      {
        path: 'sin-acceso',
        title: 'Sin acceso · Panel',
        loadComponent: () => import('./pages/access-denied/access-denied-page.component').then(m => m.AccessDeniedPageComponent)
      }
    ]
  }
];
