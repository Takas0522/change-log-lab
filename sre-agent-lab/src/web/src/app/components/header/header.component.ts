import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-header',
  imports: [RouterLink],
  template: `
    <header class="bg-white shadow">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 flex justify-between items-center">
        <a routerLink="/todos" class="text-xl font-bold text-gray-900 hover:text-gray-700">
          SRE Agent Lab ToDo
        </a>
        <div class="flex items-center gap-4">
          <span class="text-sm text-gray-600">{{ authService.user()?.displayName || authService.user()?.email }}</span>
          <button
            (click)="onLogout()"
            class="text-sm text-gray-500 hover:text-gray-700 border border-gray-300 rounded-md px-3 py-1"
          >
            ログアウト
          </button>
        </div>
      </div>
    </header>
  `
})
export class HeaderComponent {
  readonly authService = inject(AuthService);

  onLogout() {
    this.authService.logout();
  }
}
