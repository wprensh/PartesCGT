import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { shouldAttachToken } from './auth-urls';
import { AuthService } from './auth.service';

/** Adjunta el token a las llamadas a /api y cierra sesión si el backend responde 401. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.token();
  const isApi = shouldAttachToken(req.url);
  const request = token && isApi ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(request).pipe(
    catchError((e: HttpErrorResponse) => {
      if (e.status === 401 && token && isApi) auth.logout(true);
      return throwError(() => e);
    })
  );
};
