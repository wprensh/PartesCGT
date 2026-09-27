import { ChangeDetectionStrategy, Component, ElementRef, inject, input, output, signal, viewChild } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '@core/auth/auth.service';
import { describeHttpError } from '@core/http/http-error';
import { AdminApi } from '../../data/admin.api';
import { AdminRole, AdminUser, PASSWORD_MIN_LENGTH, passwordProblem } from '../../domain/access.model';

export interface UserSaved { user: AdminUser; created: boolean; }

/** Crear o editar un usuario del panel, y restablecer su contraseña. */
@Component({
  selector: 'app-user-form-dialog',
  imports: [ReactiveFormsModule],
  templateUrl: './user-form-dialog.component.html',
  styleUrl: './user-form-dialog.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserFormDialogComponent {
  private api = inject(AdminApi);
  private auth = inject(AuthService);
  private fb = inject(NonNullableFormBuilder);
  private dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  roles = input.required<AdminRole[]>();
  saved = output<UserSaved>();

  protected readonly minPassword = PASSWORD_MIN_LENGTH;
  protected editing = signal<AdminUser | null>(null);
  /** Nadie cambia su propio rol ni se desactiva (el backend también lo impide). */
  protected isSelf = signal(false);
  protected saving = signal(false);
  protected error = signal('');
  protected resetMessage = signal('');

  protected form = this.fb.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
    fullName: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(120)]],
    roleId: [0, Validators.min(1)],
    isActive: [true],
    password: ['']
  });
  protected newPassword = this.fb.control('');

  open(user?: AdminUser) {
    this.editing.set(user ?? null);
    this.isSelf.set(!!user && user.id === this.auth.profile()?.id);
    this.form.reset({
      email: user?.email ?? '', fullName: user?.fullName ?? '', roleId: user?.roleId ?? 0,
      isActive: user?.isActive ?? true, password: ''
    });
    if (user) this.form.controls.email.disable(); else this.form.controls.email.enable();
    if (this.isSelf()) { this.form.controls.roleId.disable(); this.form.controls.isActive.disable(); }
    else { this.form.controls.roleId.enable(); this.form.controls.isActive.enable(); }
    this.newPassword.reset('');
    this.error.set('');
    this.resetMessage.set('');
    this.dialog().nativeElement.showModal();
  }

  protected close() {
    this.dialog().nativeElement.close();
  }

  protected save() {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const value = this.form.getRawValue();
    const user = this.editing();

    if (!user) {
      const problem = passwordProblem(value.password);
      if (problem) return this.error.set(`Contraseña: ${problem}`);
    }

    this.saving.set(true);
    this.error.set('');
    const request = user
      ? this.api.updateUser(user.id, { fullName: value.fullName, roleId: value.roleId, isActive: value.isActive })
      : this.api.createUser({ email: value.email, fullName: value.fullName, roleId: value.roleId, password: value.password });

    request.subscribe({
      next: saved => {
        this.saving.set(false);
        this.saved.emit({ user: saved, created: !user });
        this.close();
      },
      error: (e: HttpErrorResponse) => {
        this.error.set(describeHttpError(e));
        this.saving.set(false);
      }
    });
  }

  /** Fija una contraseña nueva. El usuario queda desconectado y entra con la nueva. */
  protected resetPassword() {
    const user = this.editing();
    const password = this.newPassword.value;
    const problem = passwordProblem(password);
    if (!user) return;
    if (problem) return this.resetMessage.set(problem);

    this.api.resetUserPassword(user.id, password).subscribe({
      next: () => {
        this.newPassword.reset('');
        this.resetMessage.set('Contraseña actualizada. Compártela con el usuario por un canal seguro.');
      },
      error: (e: HttpErrorResponse) => this.resetMessage.set(describeHttpError(e))
    });
  }
}
