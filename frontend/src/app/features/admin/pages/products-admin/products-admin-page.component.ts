import { ChangeDetectionStrategy, Component, computed, inject, signal, viewChild } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatTableModule } from '@angular/material/table';
import { RouterLink } from '@angular/router';
import { AuthService } from '@core/auth/auth.service';
import { PERMISSIONS } from '@core/auth/permissions';
import { describeHttpError } from '@core/http/http-error';
import { LOW_STOCK, safeImageUrl, type Category, type Product } from '@features/catalog';
import { CopPipe } from '@shared/pipes/cop.pipe';
import { paginatedList } from '@shared/table/paginated-list';
import { trackById } from '@shared/table/pagination';
import { AdminApi } from '../../data/admin.api';
import { categoryOptions } from '../../domain/category-tree';
import { Supplier } from '../../domain/supplier.model';
import { ProductFormDialogComponent, ProductSaved } from '../../ui/product-form-dialog/product-form-dialog.component';

@Component({
  selector: 'app-products-admin-page',
  imports: [CopPipe, FormsModule, MatPaginatorModule, MatTableModule, ProductFormDialogComponent, RouterLink],
  templateUrl: './products-admin-page.component.html',
  styleUrl: './products-admin-page.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProductsAdminPageComponent {
  private api = inject(AdminApi);
  private auth = inject(AuthService);
  private formDialog = viewChild.required(ProductFormDialogComponent);

  protected readonly canManage = this.auth.can(PERMISSIONS.productsManage);
  protected readonly canViewSuppliers = this.auth.can(PERMISSIONS.suppliersView);
  protected readonly canManageSuppliers = this.auth.can(PERMISSIONS.suppliersManage);
  protected suppliers = signal<Supplier[]>([]);

  protected products = signal<Product[]>([]);
  protected categories = signal<Category[]>([]);
  protected loading = signal(true);
  protected listError = signal('');
  protected readonly lowStock = LOW_STOCK;
  protected readonly safeImageUrl = safeImageUrl;

  protected search = signal('');
  protected categoryFilter = signal(0);
  protected options = computed(() => categoryOptions(this.categories()));

  protected filtered = computed(() => {
    const q = this.search().trim().toLowerCase();
    const cat = this.categoryFilter();
    return this.products().filter(p =>
      (!cat || p.categoryId === cat || p.parentCategoryId === cat) &&
      (!q || p.name.toLowerCase().includes(q) || p.description.toLowerCase().includes(q)));
  });
  protected readonly page = paginatedList(this.filtered);
  protected readonly trackById = trackById;
  protected readonly columns = ['product', 'category', 'price', 'stock', 'status', 'actions'];

  protected brands = computed(() => unique(this.products().map(p => p.brand)));
  protected attributeNames = computed(() => unique(this.products().flatMap(p => p.attributes.map(a => a.name))));

  constructor() {
    this.api.categories().subscribe({ next: c => this.categories.set(c) });
    if (this.canViewSuppliers) this.api.suppliers().subscribe({ next: s => this.suppliers.set(s) });
    this.api.products().subscribe({
      next: list => { this.products.set(list); this.loading.set(false); },
      error: (e: HttpErrorResponse) => { this.listError.set(describeHttpError(e)); this.loading.set(false); }
    });
  }

  /** Cambiar un filtro vuelve a la primera página. */
  protected setSearch(value: string) {
    this.search.set(value);
    this.page.reset();
  }

  protected setCategoryFilter(id: number) {
    this.categoryFilter.set(id);
    this.page.reset();
  }

  protected openNew() {
    this.formDialog().open(undefined, this.categoryFilter());
  }

  protected openEdit(product: Product) {
    this.formDialog().open(product);
  }

  protected onSaved({ product, created }: ProductSaved) {
    this.products.update(list => created ? [...list, product] : list.map(p => p.id === product.id ? product : p));
  }

  protected remove(product: Product) {
    if (!confirm(`¿Eliminar "${product.name}"? Esta acción no se puede deshacer.`)) return;
    this.listError.set('');
    this.api.deleteProduct(product.id).subscribe({
      next: () => this.products.update(list => list.filter(p => p.id !== product.id)),
      error: (e: HttpErrorResponse) => this.listError.set(describeHttpError(e))
    });
  }
}

function unique(values: string[]): string[] {
  return [...new Set(values.filter(Boolean))].sort();
}
