import { ChangeDetectionStrategy, Component, inject, signal, viewChild } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { forkJoin } from 'rxjs';
import { AuthService } from '@core/auth/auth.service';
import { describeHttpError } from '@core/http/http-error';
import { DateTimePipe } from '@shared/pipes/date-time.pipe';
import { AdminApi } from '../../data/admin.api';
import { AdminRole, AdminUser } from '../../domain/access.model';
import { UserFormDialogComponent, UserSaved } from '../../ui/user-form-dialog/user-form-dialog.component';

@Component({
  selector: 'app-users-admin-page',
  imports: [DateTimePipe, UserFormDialogComponent],
  templateUrl: './users-admin-page.component.html',
  styleUrl: './users-admin-page.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UsersAdminPageComponent {
  private api = inject(AdminApi);
  private formDialog = viewChild.required(UserFormDialogComponent);
  protected currentUserId = inject(AuthService).profile()?.id;

  protected users = signal<AdminUser[]>([]);
  protected roles = signal<AdminRole[]>([]);
  protected loading = signal(true);
  protected error = signal('');

  constructor() {
    forkJoin({ users: this.api.users(), roles: this.api.roles() }).subscribe({
      next: ({ users, roles }) => { this.users.set(users); this.roles.set(roles); this.loading.set(false); },
      error: (e: HttpErrorResponse) => { this.error.set(describeHttpError(e)); this.loading.set(false); }
    });
  }

  protected openNew() {
    this.formDialog().open();
  }

  protected edit(user: AdminUser) {
    this.formDialog().open(user);
  }

  protected onSaved({ user, created }: UserSaved) {
    this.users.update(list => created ? [...list, user] : list.map(u => u.id === user.id ? user : u));
  }

  protected remove(user: AdminUser) {
    if (!confirm(`¿Eliminar a ${user.fullName} (${user.email})? Perderá el acceso al panel.`)) return;
    this.error.set('');
    this.api.deleteUser(user.id).subscribe({
      next: () => this.users.update(list => list.filter(u => u.id !== user.id)),
      error: (e: HttpErrorResponse) => this.error.set(describeHttpError(e))
    });
  }
}
