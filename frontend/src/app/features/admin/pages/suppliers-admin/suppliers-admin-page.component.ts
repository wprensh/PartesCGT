import { ChangeDetectionStrategy, Component, computed, inject, signal, viewChild } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatTableModule } from '@angular/material/table';
import { AuthService } from '@core/auth/auth.service';
import { PERMISSIONS } from '@core/auth/permissions';
import { describeHttpError } from '@core/http/http-error';
import { paginatedList } from '@shared/table/paginated-list';
import { trackById } from '@shared/table/pagination';
import { normalize } from '@shared/utils/text';
import { AdminApi } from '../../data/admin.api';
import { Supplier } from '../../domain/supplier.model';
import { SupplierFormDialogComponent, SupplierSaved } from '../../ui/supplier-form-dialog/supplier-form-dialog.component';

type StatusFilter = 'all' | 'active' | 'inactive';

@Component({
  selector: 'app-suppliers-admin-page',
  imports: [FormsModule, MatPaginatorModule, MatTableModule, SupplierFormDialogComponent],
  templateUrl: './suppliers-admin-page.component.html',
  styleUrl: './suppliers-admin-page.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SuppliersAdminPageComponent {
  private api = inject(AdminApi);
  private auth = inject(AuthService);
  private formDialog = viewChild.required(SupplierFormDialogComponent);

  protected readonly canManage = this.auth.can(PERMISSIONS.suppliersManage);
  protected suppliers = signal<Supplier[]>([]);
  protected loading = signal(true);
  protected error = signal('');
  protected search = signal('');
  protected status = signal<StatusFilter>('all');

  protected filtered = computed(() => {
    const q = normalize(this.search());
    const status = this.status();
    return this.suppliers().filter(s =>
      (status === 'all' || (status === 'active') === s.isActive) &&
      (!q || [s.name, s.taxId, s.contactName, s.city, s.email].some(v => v && normalize(v).includes(q))));
  });
  protected readonly page = paginatedList(this.filtered);
  protected readonly trackById = trackById;
  protected readonly columns = ['supplier', 'taxId', 'contact', 'city', 'products', 'status', 'actions'];

  constructor() {
    this.api.suppliers().subscribe({
      next: list => { this.suppliers.set(list); this.loading.set(false); },
      error: (e: HttpErrorResponse) => { this.error.set(describeHttpError(e)); this.loading.set(false); }
    });
  }

  /** Cambiar un filtro vuelve a la primera página. */
  protected setSearch(value: string) {
    this.search.set(value);
    this.page.reset();
  }

  protected setStatus(value: StatusFilter) {
    this.status.set(value);
    this.page.reset();
  }

  protected openNew() {
    this.formDialog().open();
  }

  protected open(supplier: Supplier) {
    this.formDialog().open(supplier);
  }

  protected onSaved({ supplier, created }: SupplierSaved) {
    this.suppliers.update(list => created
      ? [...list, supplier].sort((a, b) => a.name.localeCompare(b.name, 'es'))
      : list.map(s => s.id === supplier.id ? supplier : s));
  }

  protected remove(supplier: Supplier) {
    if (!confirm(`¿Eliminar el proveedor "${supplier.name}"?`)) return;
    this.error.set('');
    this.api.deleteSupplier(supplier.id).subscribe({
      next: () => this.suppliers.update(list => list.filter(s => s.id !== supplier.id)),
      error: (e: HttpErrorResponse) => this.error.set(describeHttpError(e))
    });
  }
}
