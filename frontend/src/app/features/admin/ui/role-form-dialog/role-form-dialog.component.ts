import { ChangeDetectionStrategy, Component, ElementRef, computed, inject, input, output, signal, viewChild } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { describeHttpError } from '@core/http/http-error';
import { AdminApi } from '../../data/admin.api';
import { AdminRole, PermissionDefinition } from '../../domain/access.model';
import { groupByModule, isImpliedOnly, togglePermission, withImplied } from '../../domain/permission-grid';

export interface RoleSaved { role: AdminRole; created: boolean; }

/** Crear o editar un rol con su grilla de permisos. El rol de sistema solo permite cambiar nombre y descripción. */
@Component({
  selector: 'app-role-form-dialog',
  imports: [ReactiveFormsModule],
  templateUrl: './role-form-dialog.component.html',
  styleUrl: './role-form-dialog.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RoleFormDialogComponent {
  private api = inject(AdminApi);
  private dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  catalog = input.required<PermissionDefinition[]>();
  saved = output<RoleSaved>();

  protected editing = signal<AdminRole | null>(null);
  protected selected = signal<string[]>([]);
  protected saving = signal(false);
  protected error = signal('');

  protected modules = computed(() => groupByModule(this.catalog()));
  protected effective = computed(() =>
    this.editing()?.isSystem ? new Set(this.catalog().map(p => p.code)) : withImplied(this.selected(), this.catalog()));

  protected form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(60)]],
    description: ['', Validators.maxLength(200)]
  });

  open(role?: AdminRole) {
    this.editing.set(role ?? null);
    this.selected.set(role?.permissions ?? []);
    this.form.reset({ name: role?.name ?? '', description: role?.description ?? '' });
    this.error.set('');
    this.dialog().nativeElement.showModal();
  }

  protected isLocked(code: string): boolean {
    return !!this.editing()?.isSystem || isImpliedOnly(code, this.selected(), this.catalog());
  }

  protected toggle(code: string) {
    if (this.isLocked(code)) return;
    this.selected.update(list => togglePermission(list, code, this.catalog()));
  }

  protected close() {
    this.dialog().nativeElement.close();
  }

  protected save() {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    const role = this.editing();
    const { name, description } = this.form.getRawValue();
    const input = { name, description: description.trim() || null, permissions: this.selected() };
    this.saving.set(true);
    this.error.set('');
    (role ? this.api.updateRole(role.id, input) : this.api.createRole(input)).subscribe({
      next: saved => {
        this.saving.set(false);
        this.saved.emit({ role: saved, created: !role });
        this.close();
      },
      error: (e: HttpErrorResponse) => {
        this.error.set(describeHttpError(e));
        this.saving.set(false);
      }
    });
  }
}
