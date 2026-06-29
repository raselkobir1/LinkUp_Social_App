import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';

let isRefreshing = false;

export const jwtInterceptor: HttpInterceptorFn = (req: HttpRequest<unknown>, next: HttpHandlerFn) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const token = authService.getAccessToken();
  const authReq = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(authReq).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401 && !isRefreshing && authService.getRefreshToken()) {
        isRefreshing = true;
        return authService.refreshToken().pipe(
          switchMap(res => {
            isRefreshing = false;
            if (res.success) {
              const retryReq = req.clone({ setHeaders: { Authorization: `Bearer ${res.data.accessToken}` } });
              return next(retryReq);
            }
            router.navigate(['/auth/login']);
            return throwError(() => err);
          }),
          catchError(refreshErr => {
            isRefreshing = false;
            router.navigate(['/auth/login']);
            return throwError(() => refreshErr);
          })
        );
      }
      return throwError(() => err);
    })
  );
};
