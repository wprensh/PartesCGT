import { ChangeDetectionStrategy, Component, inject, signal, viewChild } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { forkJoin } from 'rxjs';
import { describeHttpError } from '@core/http/http-error';
import { AdminApi } from '../../data/admin.api';
import { AdminRole, PermissionDefinition } from '../../domain/access.model';
import { RoleFormDialogComponent, RoleSaved } from '../../ui/role-form-dialog/role-form-dialog.component';

@Component({
  selector: 'app-roles-admin-page',
  imports: [RoleFormDialogComponent],
  templateUrl: './roles-admin-page.component.html',
  styleUrl: './roles-admin-page.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RolesAdminPageComponent {
  private api = inject(AdminApi);
  private formDialog = viewChild.required(RoleFormDialogComponent);

  protected roles = signal<AdminRole[]>([]);
  protected catalog = signal<PermissionDefinition[]>([]);
  protected loading = signal(true);
  protected error = signal('');

  constructor() {
    forkJoin({ roles: this.api.roles(), catalog: this.api.permissionCatalog() }).subscribe({
      next: ({ roles, catalog }) => { this.roles.set(roles); this.catalog.set(catalog); this.loading.set(false); },
      error: (e: HttpErrorResponse) => { this.error.set(describeHttpError(e)); this.loading.set(false); }
    });
  }

  /** "Productos: Ver, Crear, editar y eliminar · Proveedores: Ver" para la tarjeta del rol. */
  protected summary(role: AdminRole): string {
    if (role.isSystem) return 'Todos los permisos';
    const byModule = new Map<string, string[]>();
    for (const p of this.catalog().filter(p => role.effectivePermissions.includes(p.code)))
      byModule.set(p.module, [...(byModule.get(p.module) ?? []), p.label]);
    return [...byModule].map(([module, labels]) => `${module}: ${labels.join(', ')}`).join(' · ') || 'Sin permisos';
  }

  protected openNew() {
    this.formDialog().open();
  }

  protected edit(role: AdminRole) {
    this.formDialog().open(role);
  }

  protected onSaved({ role, created }: RoleSaved) {
    this.roles.update(list => created ? [...list, role] : list.map(r => r.id === role.id ? role : r));
  }

  protected remove(role: AdminRole) {
    if (!confirm(`¿Eliminar el rol "${role.name}"?`)) return;
    this.error.set('');
    this.api.deleteRole(role.id).subscribe({
      next: () => this.roles.update(list => list.filter(r => r.id !== role.id)),
      error: (e: HttpErrorResponse) => this.error.set(describeHttpError(e))
    });
  }
}
