import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { HeaderComponent } from '../header/header.component';
import { TodoService } from '../../services/todo.service';
import { TodoItem } from '../../models';

@Component({
  selector: 'app-todo-detail',
  imports: [FormsModule, DatePipe, HeaderComponent],
  template: `
    <app-header />
    <div class="max-w-3xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
      @if (loading()) {
        <div class="text-center py-8 text-gray-500">読み込み中...</div>
      } @else if (todo()) {
        <div class="flex justify-between items-center mb-6">
          <h1 class="text-2xl font-bold text-gray-900">ToDo詳細</h1>
          <button
            (click)="goBack()"
            class="text-sm text-gray-600 hover:text-gray-800 border border-gray-300 rounded-md px-3 py-1"
          >
            一覧に戻る
          </button>
        </div>

        @if (error()) {
          <div class="rounded-md bg-red-50 p-4 mb-4">
            <p class="text-sm text-red-800">{{ error() }}</p>
          </div>
        }

        <div class="bg-white shadow rounded-lg p-6 space-y-6">
          <div>
            <label for="title" class="block text-sm font-medium text-gray-700 mb-1">タイトル</label>
            <input
              id="title"
              type="text"
              [(ngModel)]="title"
              required
              maxlength="255"
              class="w-full border border-gray-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>

          <div>
            <label for="body" class="block text-sm font-medium text-gray-700 mb-1">本文</label>
            <textarea
              id="body"
              [(ngModel)]="body"
              rows="5"
              class="w-full border border-gray-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            ></textarea>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-6">
            <div>
              <label for="status" class="block text-sm font-medium text-gray-700 mb-1">ステータス</label>
              <select
                id="status"
                [(ngModel)]="status"
                class="w-full border border-gray-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
              >
                <option value="未着手">未着手</option>
                <option value="着手中">着手中</option>
                <option value="完了">完了</option>
              </select>
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

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-6 bg-gray-50 rounded-md p-4">
            <div>
              <span class="block text-sm font-medium text-gray-500">作成日時</span>
              <span class="text-sm text-gray-900">{{ todo()!.createdAt | date:'yyyy/MM/dd HH:mm' }}</span>
            </div>
            <div>
              <span class="block text-sm font-medium text-gray-500">完了日</span>
              <span class="text-sm text-gray-900">
                @if (todo()!.completedAt) {
                  {{ todo()!.completedAt | date:'yyyy/MM/dd HH:mm' }}
                } @else {
                  -
                }
              </span>
            </div>
          </div>

          <div class="flex justify-between items-center pt-4 border-t">
            <button
              (click)="onDelete()"
              class="bg-red-600 text-white px-4 py-2 rounded-md hover:bg-red-700 text-sm font-medium"
            >
              削除
            </button>
            <button
              (click)="onSave()"
              [disabled]="saving()"
              class="bg-blue-600 text-white px-6 py-2 rounded-md hover:bg-blue-700 text-sm font-medium disabled:opacity-50"
            >
              @if (saving()) {
                保存中...
              } @else {
                保存
              }
            </button>
          </div>
        </div>
      } @else {
        <div class="text-center py-8 text-gray-500">ToDoが見つかりません。</div>
      }
    </div>
  `
})
export class TodoDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly todoService = inject(TodoService);

  readonly todo = signal<TodoItem | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  title = '';
  body = '';
  status = '';
  dueDate = '';

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.router.navigate(['/todos']);
      return;
    }

    this.loading.set(true);
    this.todoService.getTodo(id).subscribe({
      next: (todo) => {
        this.todo.set(todo);
        this.title = todo.title;
        this.body = todo.body || '';
        this.status = todo.status;
        this.dueDate = todo.dueDate ? todo.dueDate.split('T')[0] : '';
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.router.navigate(['/todos']);
      }
    });
  }

  onSave() {
    if (!this.title.trim()) {
      this.error.set('タイトルは必須です。');
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    const todo = this.todo();
    if (!todo) return;

    this.todoService.updateTodo(todo.id, {
      title: this.title,
      body: this.body,
      status: this.status,
      dueDate: this.dueDate || undefined
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.router.navigate(['/todos']);
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(err.error?.message || '保存に失敗しました。');
      }
    });
  }

  onDelete() {
    const todo = this.todo();
    if (!todo) return;

    if (!confirm('このToDoを削除しますか？')) return;

    this.todoService.deleteTodo(todo.id).subscribe({
      next: () => this.router.navigate(['/todos']),
      error: (err) => this.error.set(err.error?.message || '削除に失敗しました。')
    });
  }

  goBack() {
    this.router.navigate(['/todos']);
  }
}
