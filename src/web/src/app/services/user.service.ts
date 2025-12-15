import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { UserProfile } from '../models';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private readonly http = inject(HttpClient);
  private readonly API_URL = 'http://localhost:5000/api/users';
  
  private readonly profileSignal = signal<UserProfile | null>(null);
  
  readonly profile = this.profileSignal.asReadonly();

  loadProfile() {
    return this.http.get<UserProfile>(`${this.API_URL}/me`).pipe(
      tap(profile => this.profileSignal.set(profile))
    );
  }

  updateProfile(displayName: string) {
    return this.http.put<UserProfile>(`${this.API_URL}/me`, { displayName }).pipe(
      tap(profile => this.profileSignal.set(profile))
    );
  }

  searchUsers(query: string) {
    return this.http.get<UserProfile[]>(`${this.API_URL}/search`, {
      params: { q: query }
    });
  }
}
