import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HeaderComponent } from '../header/header.component';
import { TodoService } from '../../services/todo.service';

@Component({
  selector: 'app-todo-create',
  imports: [FormsModule, HeaderComponent],
  template: `
    <app-header />
    <div class="max-w-3xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
      <div class="flex justify-between items-center mb-6">
        <h1 class="text-2xl font-bold text-gray-900">ToDo新規作成</h1>
        <button
          (click)="goBack()"
          class="text-sm text-gray-600 hover:text-gray-800 border border-gray-300 rounded-md px-3 py-1"
        >
          キャンセル
        </button>
      </div>

      @if (error()) {
        <div class="rounded-md bg-red-50 p-4 mb-4">
          <p class="text-sm text-red-800">{{ error() }}</p>
        </div>
      }

      <div class="bg-white shadow rounded-lg p-6 space-y-6">
        <div>
          <label for="title" class="block text-sm font-medium text-gray-700 mb-1">タイトル <span class="text-red-500">*</span></label>
          <input
            id="title"
            type="text"
            [(ngModel)]="title"
            required
            maxlength="255"
            class="w-full border border-gray-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            placeholder="ToDoのタイトルを入力"
          />
        </div>

        <div>
          <label for="body" class="block text-sm font-medium text-gray-700 mb-1">本文</label>
          <textarea
            id="body"
            [(ngModel)]="body"
            rows="5"
            class="w-full border border-gray-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            placeholder="詳細な説明を入力（任意）"
          ></textarea>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-6">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">ステータス</label>
            <div class="px-3 py-2 bg-gray-100 rounded-md text-sm text-gray-600">未着手</div>
          </div>

          <div>
            <label for="dueDate" class="block text-sm font-medium text-gray-700 mb-1">完了予定日</label>
            <input
              id="dueDate"
              type="date"
              [(ngModel)]="dueDate"
              class="w-full border border-gray-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
        </div>

        <div class="flex justify-end pt-4 border-t">
          <button
            (click)="onSave()"
            [disabled]="saving()"
            class="bg-blue-600 text-white px-6 py-2 rounded-md hover:bg-blue-700 text-sm font-medium disabled:opacity-50"
          >
            @if (saving()) {
              作成中...
            } @else {
              作成
            }
          </button>
        </div>
      </div>
    </div>
  `
})
export class TodoCreateComponent {
  private readonly router = inject(Router);
  private readonly todoService = inject(TodoService);

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  title = '';
  body = '';
  dueDate = '';

  onSave() {
    if (!this.title.trim()) {
      this.error.set('タイトルは必須です。');
      return;
    }

    if (this.title.length > 255) {
      this.error.set('タイトルは255文字以内で入力してください。');
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    this.todoService.createTodo({
      title: this.title,
      body: this.body || undefined,
      dueDate: this.dueDate || undefined
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.router.navigate(['/todos']);
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(err.error?.message || '作成に失敗しました。');
      }
    });
  }

  goBack() {
    this.router.navigate(['/todos']);
  }
}
