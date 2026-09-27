import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '@core/auth/auth.service';
import { describeHttpError } from '@core/http/http-error';

@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule],
  templateUrl: './login-page.component.html',
  styleUrl: './login-page.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginPageComponent {
  private auth = inject(AuthService);
  private router = inject(Router);

  // Query params enlazados por withComponentInputBinding()
  volver = input<string>();
  expirada = input<string>();

  protected form = inject(NonNullableFormBuilder).group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  protected sending = signal(false);
  protected error = signal('');

  protected submit() {
    if (this.form.invalid) return;
    this.sending.set(true);
    this.error.set('');
    const { email, password } = this.form.getRawValue();

    this.auth.login(email, password).subscribe({
      next: () => this.router.navigateByUrl(this.volver() || '/admin'),
      error: (e: HttpErrorResponse) => {
        this.error.set(e.status === 401 ? 'Correo o contraseña incorrectos.' : describeHttpError(e));
        this.sending.set(false);
      }
    });
  }
}
