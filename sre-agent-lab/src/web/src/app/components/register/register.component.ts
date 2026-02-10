import { Component, signal, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div class="max-w-md w-full space-y-8">
        <div>
          <h2 class="mt-6 text-center text-3xl font-extrabold text-gray-900">
            アカウント登録
          </h2>
          <p class="mt-2 text-center text-sm text-gray-600">
            SRE Agent Lab ToDo
          </p>
        </div>
        <form class="mt-8 space-y-6" (ngSubmit)="onSubmit()">
          @if (error()) {
            <div class="rounded-md bg-red-50 p-4">
              <p class="text-sm text-red-800">{{ error() }}</p>
            </div>
          }
          @if (success()) {
            <div class="rounded-md bg-green-50 p-4">
              <p class="text-sm text-green-800">{{ success() }}</p>
            </div>
          }
          <div class="rounded-md shadow-sm -space-y-px">
            <div>
              <label for="displayName" class="sr-only">表示名</label>
              <input
                id="displayName"
                name="displayName"
                type="text"
                [(ngModel)]="displayName"
                class="appearance-none rounded-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-t-md focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10 sm:text-sm"
                placeholder="表示名（任意）"
              />
            </div>
            <div>
              <label for="email" class="sr-only">メールアドレス</label>
              <input
                id="email"
                name="email"
                type="email"
                required
                [(ngModel)]="email"
                class="appearance-none rounded-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10 sm:text-sm"
                placeholder="メールアドレス"
              />
            </div>
            <div>
              <label for="password" class="sr-only">パスワード</label>
              <input
                id="password"
                name="password"
                type="password"
                required
                [(ngModel)]="password"
                class="appearance-none rounded-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-b-md focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10 sm:text-sm"
                placeholder="パスワード（8文字以上）"
              />
            </div>
          </div>

          <div>
            <button
              type="submit"
              [disabled]="loading()"
              class="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50"
            >
              @if (loading()) {
                <span>登録中...</span>
              } @else {
                <span>登録</span>
              }
            </button>
          </div>

          <div class="text-center">
            <a routerLink="/login" class="text-blue-600 hover:text-blue-500">
              アカウントをお持ちの方はこちら
            </a>
          </div>
        </form>
      </div>
    </div>
  `
})
export class RegisterComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  displayName = '';
  email = '';
  password = '';
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly success = signal<string | null>(null);

  onSubmit() {
    if (!this.email || !this.password) {
      this.error.set('メールアドレスとパスワードは必須です。');
      return;
    }

    if (this.password.length < 8) {
      this.error.set('パスワードは8文字以上で入力してください。');
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.success.set(null);

    this.authService.register(this.email, this.password, this.displayName || undefined).subscribe({
      next: () => {
        this.loading.set(false);
        this.success.set('登録が完了しました。ログインしてください。');
        setTimeout(() => this.router.navigate(['/login']), 1500);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.message || '登録に失敗しました。');
      }
    });
  }
}
