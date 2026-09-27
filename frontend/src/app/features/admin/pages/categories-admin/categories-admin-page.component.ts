import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatTableModule } from '@angular/material/table';
import { Observable } from 'rxjs';
import { AuthService } from '@core/auth/auth.service';
import { PERMISSIONS } from '@core/auth/permissions';
import { describeHttpError } from '@core/http/http-error';
import { paginatedList } from '@shared/table/paginated-list';
import { trackById } from '@shared/table/pagination';
import type { Category } from '@features/catalog';
import { AdminApi } from '../../data/admin.api';
import { categoryTree, describeCategory, hasChildren, mainCategories } from '../../domain/category-tree';

const MIN_NAME_LENGTH = 2;

@Component({
  selector: 'app-categories-admin-page',
  imports: [FormsModule, MatPaginatorModule, MatTableModule],
  templateUrl: './categories-admin-page.component.html',
  styleUrl: './categories-admin-page.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CategoriesAdminPageComponent {
  private api = inject(AdminApi);
  protected readonly canManage = inject(AuthService).can(PERMISSIONS.categoriesManage);

  protected categories = signal<Category[]>([]);
  protected loading = signal(true);
  protected busy = signal(false);
  protected error = signal('');

  protected newName = signal('');
  protected newParent = signal<number | null>(null);
  protected editingId = signal<number | null>(null);
  protected editName = signal('');
  protected editParent = signal<number | null>(null);

  protected parents = computed(() => mainCategories(this.categories()));
  protected tree = computed(() => categoryTree(this.categories()));
  /** Árbol paginado: las subcategorías siguen a su principal (aunque puedan pasar a la página siguiente). */
  protected readonly page = paginatedList(this.tree);
  protected readonly trackById = trackById;
  protected readonly columns = ['name', 'location', 'detail', 'actions'];
  private parentNames = computed(() => new Map(this.categories().map(c => [c.id, c.name])));

  protected locationOf = (c: Category) =>
    c.parentId === null ? 'Principal' : `Subcategoría de ${this.parentNames().get(c.parentId) ?? '—'}`;

  protected describe = (c: Category) => describeCategory(this.categories(), c);
  protected hasChildren = (c: Category) => hasChildren(this.categories(), c);
  protected isValidName = (name: string) => name.trim().length >= MIN_NAME_LENGTH;

  constructor() {
    this.load();
  }

  protected create() {
    this.run(this.api.createCategory(this.newName().trim(), this.newParent()), () => {
      this.newName.set('');
      this.newParent.set(null);
    });
  }

  protected startEdit(c: Category) {
    this.editName.set(c.name);
    this.editParent.set(c.parentId);
    this.editingId.set(c.id);
  }

  protected save(c: Category) {
    if (!this.isValidName(this.editName()) || this.busy()) return;
    this.run(this.api.updateCategory(c.id, this.editName().trim(), this.editParent()), () => this.editingId.set(null));
  }

  protected remove(c: Category) {
    if (!confirm(`¿Eliminar la categoría "${c.name}"?`)) return;
    this.run(this.api.deleteCategory(c.id));
  }

  private load() {
    this.api.categories().subscribe({
      next: list => { this.categories.set(list); this.loading.set(false); },
      error: (e: HttpErrorResponse) => { this.error.set(describeHttpError(e)); this.loading.set(false); }
    });
  }

  /** Ejecuta una escritura y recarga la lista (los conteos los calcula el backend). */
  private run(request: Observable<unknown>, onSuccess?: () => void) {
    this.busy.set(true);
    this.error.set('');
    request.subscribe({
      next: () => { onSuccess?.(); this.busy.set(false); this.load(); },
      error: (e: HttpErrorResponse) => { this.error.set(describeHttpError(e)); this.busy.set(false); }
    });
  }
}
