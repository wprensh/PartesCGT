import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Category, Product, ProductQuery, toParams } from '@features/catalog';
import { AdminRole, AdminUser, PermissionDefinition, RoleInput, UserCreateInput, UserUpdateInput } from '../domain/access.model';
import { ProductInput } from '../domain/product-input';
import { ProductSupplier, ProductSupplierInput, SuppliedProduct, Supplier, SupplierInput } from '../domain/supplier.model';

/** Escritura del catálogo. Requiere sesión de administrador (el token lo adjunta authInterceptor). */
@Injectable({ providedIn: 'root' })
export class AdminApi {
  private http = inject(HttpClient);

  // ---------- Productos ----------
  /** Incluye productos inactivos. */
  products(query: ProductQuery = {}) {
    return this.http.get<Product[]>('/api/products/admin', { params: toParams(query) });
  }

  createProduct(input: ProductInput) {
    return this.http.post<Product>('/api/products', input);
  }

  updateProduct(id: number, input: ProductInput) {
    return this.http.put<Product>(`/api/products/${id}`, input);
  }

  deleteProduct(id: number) {
    return this.http.delete<void>(`/api/products/${id}`);
  }

  /** Sube una foto y devuelve la URL que luego se guarda en el producto. */
  uploadProductImage(file: File) {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<{ url: string }>('/api/products/images', form);
  }

  // ---------- Proveedores ----------
  suppliers() {
    return this.http.get<Supplier[]>('/api/suppliers');
  }

  supplierProducts(id: number) {
    return this.http.get<SuppliedProduct[]>(`/api/suppliers/${id}/products`);
  }

  createSupplier(input: SupplierInput) {
    return this.http.post<Supplier>('/api/suppliers', input);
  }

  updateSupplier(id: number, input: SupplierInput) {
    return this.http.put<Supplier>(`/api/suppliers/${id}`, input);
  }

  deleteSupplier(id: number) {
    return this.http.delete<void>(`/api/suppliers/${id}`);
  }

  productSuppliers(productId: number) {
    return this.http.get<ProductSupplier[]>(`/api/products/${productId}/suppliers`);
  }

  /** Reemplaza la lista completa de proveedores del producto. */
  replaceProductSuppliers(productId: number, suppliers: ProductSupplierInput[]) {
    return this.http.put<ProductSupplier[]>(`/api/products/${productId}/suppliers`, suppliers);
  }

  // ---------- Usuarios y roles ----------
  users() {
    return this.http.get<AdminUser[]>('/api/users');
  }

  createUser(input: UserCreateInput) {
    return this.http.post<AdminUser>('/api/users', input);
  }

  updateUser(id: number, input: UserUpdateInput) {
    return this.http.put<AdminUser>(`/api/users/${id}`, input);
  }

  resetUserPassword(id: number, newPassword: string) {
    return this.http.post<void>(`/api/users/${id}/password`, { newPassword });
  }

  deleteUser(id: number) {
    return this.http.delete<void>(`/api/users/${id}`);
  }

  roles() {
    return this.http.get<AdminRole[]>('/api/roles');
  }

  permissionCatalog() {
    return this.http.get<PermissionDefinition[]>('/api/roles/permissions');
  }

  createRole(input: RoleInput) {
    return this.http.post<AdminRole>('/api/roles', input);
  }

  updateRole(id: number, input: RoleInput) {
    return this.http.put<AdminRole>(`/api/roles/${id}`, input);
  }

  deleteRole(id: number) {
    return this.http.delete<void>(`/api/roles/${id}`);
  }

  // ---------- Categorías ----------
  categories() {
    return this.http.get<Category[]>('/api/categories');
  }

  createCategory(name: string, parentId: number | null = null) {
    return this.http.post<Category>('/api/categories', { name, parentId });
  }

  updateCategory(id: number, name: string, parentId: number | null) {
    return this.http.put<void>(`/api/categories/${id}`, { name, parentId });
  }

  deleteCategory(id: number) {
    return this.http.delete<void>(`/api/categories/${id}`);
  }
}
