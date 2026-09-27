import { ChangeDetectionStrategy, Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '@core/auth/auth.service';
import { describeHttpError } from '@core/http/http-error';
import { PASSWORD_MIN_LENGTH, passwordProblem } from '../../domain/access.model';

/** El usuario conectado cambia su propia contraseña. Sus otras sesiones se cierran; esta sigue abierta. */
@Component({
  selector: 'app-change-password-dialog',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <dialog #dialog aria-labelledby="password-title">
      <form [formGroup]="form" (ngSubmit)="save()" class="dlg">
        <h2 id="password-title">Cambiar mi contraseña</h2>
        <label class="field">Contraseña actual
          <input formControlName="current" type="password" autocomplete="current-password">
        </label>
        <label class="field">Contraseña nueva
          <input formControlName="next" type="password" autocomplete="new-password">
          <small>Mínimo {{ minLength }} caracteres, con letras y números.</small>
        </label>
        <label class="field">Repite la nueva
          <input formControlName="repeat" type="password" autocomplete="new-password">
        </label>
        @if (message()) { <p [class.error]="!done()" [class.ok]="done()" role="status">{{ message() }}</p> }
        <div class="buttons">
          <button type="button" class="btn btn-ghost" (click)="close()">{{ done() ? 'Cerrar' : 'Cancelar' }}</button>
          @if (!done()) { <button class="btn" [disabled]="saving()">{{ saving() ? 'Guardando…' : 'Cambiar contraseña' }}</button> }
        </div>
      </form>
    </dialog>
  `,
  styles: `
    .dlg { display: grid; gap: .9rem; padding: 1.5rem; }
    .dlg h2 { font-size: 1.4rem; }
    .buttons { display: flex; justify-content: flex-end; gap: .5rem; }
    .btn.btn-ghost { background: transparent; color: var(--ink); }
    .ok { color: var(--ink); margin: 0; }
  `
})
export class ChangePasswordDialogComponent {
  private auth = inject(AuthService);
  private dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  protected readonly minLength = PASSWORD_MIN_LENGTH;
  protected saving = signal(false);
  protected done = signal(false);
  protected message = signal('');
  protected form = inject(NonNullableFormBuilder).group({
    current: ['', Validators.required],
    next: ['', Validators.required],
    repeat: ['', Validators.required]
  });

  open() {
    this.form.reset();
    this.done.set(false);
    this.message.set('');
    this.dialog().nativeElement.showModal();
  }

  protected close() {
    this.dialog().nativeElement.close();
  }

  protected save() {
    const { current, next, repeat } = this.form.getRawValue();
    if (this.form.invalid) return this.message.set('Completa los tres campos.');
    if (next !== repeat) return this.message.set('La contraseña nueva no coincide al repetirla.');
    const problem = passwordProblem(next);
    if (problem) return this.message.set(problem);

    this.saving.set(true);
    this.auth.changePassword(current, next).subscribe({
      next: () => {
        this.saving.set(false);
        this.done.set(true);
        this.message.set('Listo. Tus otras sesiones abiertas se cerraron.');
      },
      error: (e: HttpErrorResponse) => {
        this.saving.set(false);
        this.message.set(describeHttpError(e));
      }
    });
  }
}
