import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import {
  AuthResponse, LoginDto, RegisterDto, RefreshTokenDto,
  ForgotPasswordDto, ResetPasswordDto, ChangePasswordDto, UserDto
} from '../models/auth.model';

const ACCESS_TOKEN_KEY = 'linkup_access_token';
const REFRESH_TOKEN_KEY = 'linkup_refresh_token';
const USER_KEY = 'linkup_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private _currentUser = signal<UserDto | null>(this.loadUser());
  readonly currentUser = this._currentUser.asReadonly();
  readonly isLoggedIn = computed(() => !!this._currentUser());

  constructor(private http: HttpClient, private router: Router) {}

  register(dto: RegisterDto): Observable<ApiResponse<AuthResponse>> {
    return this.http.post<ApiResponse<AuthResponse>>(`${environment.apiUrl}/auth/register`, dto).pipe(
      tap(res => { if (res.success) this.storeAuth(res.data); })
    );
  }

  login(dto: LoginDto): Observable<ApiResponse<AuthResponse>> {
    return this.http.post<ApiResponse<AuthResponse>>(`${environment.apiUrl}/auth/login`, dto).pipe(
      tap(res => { if (res.success) this.storeAuth(res.data); })
    );
  }

  logout(): Observable<ApiResponse<object>> {
    const token = this.getRefreshToken();
    return this.http.post<ApiResponse<object>>(`${environment.apiUrl}/auth/logout`, { refreshToken: token }).pipe(
      tap(() => this.clearAuth()),
      catchError(err => { this.clearAuth(); return throwError(() => err); })
    );
  }

  refreshToken(): Observable<ApiResponse<AuthResponse>> {
    return this.http.post<ApiResponse<AuthResponse>>(`${environment.apiUrl}/auth/refresh`, {
      accessToken: this.getAccessToken(),
      refreshToken: this.getRefreshToken()
    } as RefreshTokenDto).pipe(
      tap(res => { if (res.success) this.storeAuth(res.data); })
    );
  }

  forgotPassword(dto: ForgotPasswordDto): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${environment.apiUrl}/auth/forgot-password`, dto);
  }

  resetPassword(dto: ResetPasswordDto): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${environment.apiUrl}/auth/reset-password`, dto);
  }

  changePassword(dto: ChangePasswordDto): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${environment.apiUrl}/auth/change-password`, dto);
  }

  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  updateCurrentUser(user: UserDto): void {
    this._currentUser.set(user);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
  }

  private storeAuth(auth: AuthResponse): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, auth.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, auth.refreshToken);
    localStorage.setItem(USER_KEY, JSON.stringify(auth.user));
    this._currentUser.set(auth.user);
  }

  private clearAuth(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this._currentUser.set(null);
    this.router.navigate(['/auth/login']);
  }

  private loadUser(): UserDto | null {
    try {
      const raw = localStorage.getItem(USER_KEY);
      return raw ? JSON.parse(raw) : null;
    } catch { return null; }
  }
}
