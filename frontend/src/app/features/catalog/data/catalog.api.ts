import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Category } from '../domain/category.model';
import { Product } from '../domain/product.model';

export interface ProductQuery { categoryId?: number | null; q?: string; }

/** Lectura pública del catálogo. La escritura vive en la feature admin. */
@Injectable({ providedIn: 'root' })
export class CatalogApi {
  private http = inject(HttpClient);

  products(query: ProductQuery = {}) {
    return this.http.get<Product[]>('/api/products', { params: toParams(query) });
  }

  categories() {
    return this.http.get<Category[]>('/api/categories');
  }
}

export function toParams({ categoryId, q }: ProductQuery): HttpParams {
  let params = new HttpParams();
  if (categoryId) params = params.set('categoryId', categoryId);
  if (q?.trim()) params = params.set('q', q.trim());
  return params;
}
