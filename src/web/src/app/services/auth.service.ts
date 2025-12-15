import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, tap, of } from 'rxjs';
import { AuthResponse, LoginRequest, RegisterRequest, User } from '../models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly API_URL = 'http://localhost:5000/api/auth';
  
  private readonly tokenSignal = signal<string | null>(this.getStoredToken());
  private readonly userSignal = signal<User | null>(this.getStoredUser());
  
  readonly token = this.tokenSignal.asReadonly();
  readonly user = this.userSignal.asReadonly();
  readonly isAuthenticated = computed(() => !!this.tokenSignal());

  private getDeviceId(): string {
    let deviceId = localStorage.getItem('device_id');
    if (!deviceId) {
      deviceId = crypto.randomUUID();
      localStorage.setItem('device_id', deviceId);
    }
    return deviceId;
  }

  private getStoredToken(): string | null {
    return sessionStorage.getItem('auth_token');
  }

  private getStoredUser(): User | null {
    const userJson = sessionStorage.getItem('auth_user');
    return userJson ? JSON.parse(userJson) : null;
  }

  private setAuth(token: string, user: User): void {
    sessionStorage.setItem('auth_token', token);
    sessionStorage.setItem('auth_user', JSON.stringify(user));
    this.tokenSignal.set(token);
    this.userSignal.set(user);
  }

  private clearAuth(): void {
    sessionStorage.removeItem('auth_token');
    sessionStorage.removeItem('auth_user');
    this.tokenSignal.set(null);
    this.userSignal.set(null);
  }

  register(email: string, password: string, displayName: string) {
    const request: RegisterRequest = {
      email,
      password,
      displayName,
      deviceId: this.getDeviceId()
    };

    return this.http.post<AuthResponse>(`${this.API_URL}/register`, request).pipe(
      tap(response => {
        this.setAuth(response.token, response.user);
        this.router.navigate(['/lists']);
      }),
      catchError(error => {
        console.error('Registration failed:', error);
        throw error;
      })
    );
  }

  login(email: string, password: string) {
    const request: LoginRequest = {
      email,
      password,
      deviceId: this.getDeviceId()
    };

    return this.http.post<AuthResponse>(`${this.API_URL}/login`, request).pipe(
      tap(response => {
        this.setAuth(response.token, response.user);
        this.router.navigate(['/lists']);
      }),
      catchError(error => {
        console.error('Login failed:', error);
        throw error;
      })
    );
  }

  logout() {
    return this.http.post(`${this.API_URL}/logout`, {}).pipe(
      tap(() => {
        this.clearAuth();
        this.router.navigate(['/login']);
      }),
      catchError(error => {
        console.error('Logout failed:', error);
        // Clear local state even if API call fails
        this.clearAuth();
        this.router.navigate(['/login']);
        return of(null);
      })
    );
  }

  checkAuth(): boolean {
    return this.isAuthenticated();
  }
}
