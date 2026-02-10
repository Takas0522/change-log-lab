import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { HeaderComponent } from '../header/header.component';
import { TodoService } from '../../services/todo.service';
import { TodoFilter } from '../../models';

@Component({
  selector: 'app-todo-list',
  imports: [FormsModule, RouterLink, DatePipe, HeaderComponent],
  template: `
    <app-header />
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
      <div class="flex justify-between items-center mb-6">
        <h1 class="text-2xl font-bold text-gray-900">ToDo一覧</h1>
        <div class="flex gap-3">
          <button
            (click)="onBomb()"
            class="bg-red-600 text-white px-4 py-2 rounded-md hover:bg-red-700 text-sm font-medium"
          >
            💣 BOMB
          </button>
          <a
            routerLink="/todos/new"
            class="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700 text-sm font-medium"
          >
            新規作成
          </a>
        </div>
      </div>

      <!-- Filter Section -->
      <div class="bg-white shadow rounded-lg p-4 mb-6">
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4 items-end">
          <div>
            <label for="filterTitle" class="block text-sm font-medium text-gray-700 mb-1">タイトル</label>
            <input
              id="filterTitle"
              type="text"
              [(ngModel)]="filterTitle"
              (ngModelChange)="onFilterChange()"
              placeholder="キーワード検索"
              class="w-full border border-gray-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
          <div>
            <label for="filterStatus" class="block text-sm font-medium text-gray-700 mb-1">ステータス</label>
            <select
              id="filterStatus"
              [(ngModel)]="filterStatus"
              (ngModelChange)="onFilterChange()"
              class="w-full border border-gray-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="">すべて</option>
              <option value="未着手">未着手</option>
              <option value="着手中">着手中</option>
              <option value="完了">完了</option>
            </select>
          </div>
          <div>
            <label for="filterDateFrom" class="block text-sm font-medium text-gray-700 mb-1">完了予定日(開始)</label>
            <input
              id="filterDateFrom"
              type="date"
              [(ngModel)]="filterDateFrom"
              (ngModelChange)="onFilterChange()"
              class="w-full border border-gray-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
          <div>
            <label for="filterDateTo" class="block text-sm font-medium text-gray-700 mb-1">完了予定日(終了)</label>
            <input
              id="filterDateTo"
              type="date"
              [(ngModel)]="filterDateTo"
              (ngModelChange)="onFilterChange()"
              class="w-full border border-gray-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
          <div>
            <button
              (click)="clearFilters()"
              class="w-full border border-gray-300 text-gray-700 px-4 py-2 rounded-md hover:bg-gray-50 text-sm"
            >
              クリア
            </button>
          </div>
        </div>
      </div>

      <!-- Todo Table -->
      @if (loading()) {
        <div class="text-center py-8 text-gray-500">読み込み中...</div>
      } @else if (todoService.todos().length === 0) {
        <div class="text-center py-8 text-gray-500">
          ToDoがありません。新規作成してください。
        </div>
      } @else {
        <!-- Desktop Table -->
        <div class="hidden md:block bg-white shadow rounded-lg overflow-hidden">
          <table class="min-w-full divide-y divide-gray-200">
            <thead class="bg-gray-50">
              <tr>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">タイトル</th>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">ステータス</th>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">完了予定日</th>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">作成日時</th>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">完了日</th>
              </tr>
            </thead>
            <tbody class="bg-white divide-y divide-gray-200">
              @for (todo of todoService.todos(); track todo.id) {
                <tr class="hover:bg-gray-50">
                  <td class="px-6 py-4">
                    <a [routerLink]="['/todos', todo.id]" class="text-blue-600 hover:text-blue-800 font-medium">
                      {{ todo.title }}
                    </a>
                  </td>
                  <td class="px-6 py-4">
                    <select
                      [ngModel]="todo.status"
                      (ngModelChange)="onStatusChange(todo.id, $event)"
                      class="border border-gray-300 rounded-md px-2 py-1 text-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                      [class.text-gray-600]="todo.status === '未着手'"
                      [class.text-blue-600]="todo.status === '着手中'"
                      [class.text-green-600]="todo.status === '完了'"
                    >
                      <option value="未着手">未着手</option>
                      <option value="着手中">着手中</option>
                      <option value="完了">完了</option>
                    </select>
                  </td>
                  <td class="px-6 py-4 text-sm text-gray-500">
                    {{ todo.dueDate | date:'yyyy/MM/dd' }}
                  </td>
                  <td class="px-6 py-4 text-sm text-gray-500">
                    {{ todo.createdAt | date:'yyyy/MM/dd HH:mm' }}
                  </td>
                  <td class="px-6 py-4 text-sm text-gray-500">
                    {{ todo.completedAt | date:'yyyy/MM/dd HH:mm' }}
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <!-- Mobile Cards -->
        <div class="md:hidden space-y-4">
          @for (todo of todoService.todos(); track todo.id) {
            <div class="bg-white shadow rounded-lg p-4">
              <a [routerLink]="['/todos', todo.id]" class="text-blue-600 hover:text-blue-800 font-medium text-lg">
                {{ todo.title }}
              </a>
              <div class="mt-3 space-y-2">
                <div class="flex items-center justify-between">
                  <span class="text-sm text-gray-500">ステータス</span>
                  <select
                    [ngModel]="todo.status"
                    (ngModelChange)="onStatusChange(todo.id, $event)"
                    class="border border-gray-300 rounded-md px-2 py-1 text-sm"
                  >
                    <option value="未着手">未着手</option>
                    <option value="着手中">着手中</option>
                    <option value="完了">完了</option>
                  </select>
                </div>
                @if (todo.dueDate) {
                  <div class="flex justify-between text-sm">
                    <span class="text-gray-500">完了予定日</span>
                    <span>{{ todo.dueDate | date:'yyyy/MM/dd' }}</span>
                  </div>
                }
                <div class="flex justify-between text-sm">
                  <span class="text-gray-500">作成日時</span>
                  <span>{{ todo.createdAt | date:'yyyy/MM/dd HH:mm' }}</span>
                </div>
                @if (todo.completedAt) {
                  <div class="flex justify-between text-sm">
                    <span class="text-gray-500">完了日</span>
                    <span>{{ todo.completedAt | date:'yyyy/MM/dd HH:mm' }}</span>
                  </div>
                }
              </div>
            </div>
          }
        </div>
      }
    </div>
  `
})
export class TodoListComponent implements OnInit {
  readonly todoService = inject(TodoService);
  readonly loading = signal(false);

  filterTitle = '';
  filterStatus = '';
  filterDateFrom = '';
  filterDateTo = '';

  ngOnInit() {
    this.loadTodos();
  }

  onFilterChange() {
    this.loadTodos();
  }

  clearFilters() {
    this.filterTitle = '';
    this.filterStatus = '';
    this.filterDateFrom = '';
    this.filterDateTo = '';
    this.loadTodos();
  }

  onBomb() {
    this.todoService.bomb().subscribe({
      next: () => {},
      error: (err) => {
        console.error('BOMB error:', err);
        alert('サーバーエラーが発生しました！(意図的なエラー)');
      }
    });
  }

  onStatusChange(id: string, status: string) {
    this.todoService.updateStatus(id, status).subscribe({
      next: () => this.loadTodos(),
      error: (err) => console.error('Status update failed:', err)
    });
  }

  private loadTodos() {
    this.loading.set(true);
    const filter: TodoFilter = {};
    if (this.filterTitle) filter.title = this.filterTitle;
    if (this.filterStatus) filter.status = this.filterStatus;
    if (this.filterDateFrom) filter.dueDateFrom = this.filterDateFrom;
    if (this.filterDateTo) filter.dueDateTo = this.filterDateTo;

    this.todoService.loadTodos(filter).subscribe({
      next: () => this.loading.set(false),
      error: (err) => {
        this.loading.set(false);
        console.error('Failed to load todos:', err);
      }
    });
  }
}
