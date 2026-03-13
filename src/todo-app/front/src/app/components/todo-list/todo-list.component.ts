import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TodoService } from '../../services/todo.service';
import { Todo, PagedResult } from '../../models';

/**
 * Todo一覧コンポーネント
 * REQ-FUNC-001, REQ-FUNC-013～017対応
 */
@Component({
  selector: 'app-todo-list',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="todo-list-container">
      <h2>Todo一覧</h2>
      
      @if (loading()) {
        <div class="loading">読み込み中...</div>
      } @else if (error()) {
        <div class="error">エラー: {{ error() }}</div>
      } @else {
        @if (todos().length === 0) {
          <div class="empty">Todoがありません</div>
        } @else {
          <div class="todo-items">
            @for (todo of todos(); track todo.todoId) {
              <div class="todo-item">
                <h3>{{ todo.title }}</h3>
                <p>{{ todo.content }}</p>
                <div class="todo-meta">
                  <span class="status" [class]="'status-' + todo.status.toLowerCase()">
                    {{ todo.status }}
                  </span>
                  <span class="date">{{ todo.createdAt | date:'short' }}</span>
                </div>
                <div class="labels">
                  @for (label of todo.labels; track label.labelId) {
                    <span class="label" [style.background-color]="label.color">
                      {{ label.name }}
                    </span>
                  }
                </div>
              </div>
            }
          </div>
          
          <!-- ページネーション -->
          <div class="pagination">
            <button 
              [disabled]="currentPage() === 1"
              (click)="loadPage(currentPage() - 1)">
              前へ
            </button>
            <span>{{ currentPage() }} / {{ totalPages() }}</span>
            <button 
              [disabled]="currentPage() >= totalPages()"
              (click)="loadPage(currentPage() + 1)">
              次へ
            </button>
          </div>
        }
      }
    </div>
  `,
  styles: [`
    .todo-list-container {
      padding: 20px;
      max-width: 1200px;
      margin: 0 auto;
    }
    
    h2 {
      margin-bottom: 20px;
      color: #333;
    }
    
    .loading, .error, .empty {
      text-align: center;
      padding: 40px;
      color: #666;
    }
    
    .error {
      color: #d32f2f;
    }
    
    .todo-items {
      display: grid;
      gap: 16px;
    }
    
    .todo-item {
      border: 1px solid #ddd;
      border-radius: 8px;
      padding: 16px;
      background: white;
      transition: box-shadow 0.2s;
    }
    
    .todo-item:hover {
      box-shadow: 0 2px 8px rgba(0,0,0,0.1);
    }
    
    .todo-item h3 {
      margin: 0 0 8px 0;
      color: #333;
    }
    
    .todo-item p {
      margin: 0 0 12px 0;
      color: #666;
    }
    
    .todo-meta {
      display: flex;
      gap: 12px;
      align-items: center;
      margin-bottom: 8px;
    }
    
    .status {
      padding: 4px 12px;
      border-radius: 12px;
      font-size: 12px;
      font-weight: 500;
    }
    
    .status-notstarted {
      background: #e3f2fd;
      color: #1976d2;
    }
    
    .status-inprogress {
      background: #fff3e0;
      color: #f57c00;
    }
    
    .status-completed {
      background: #e8f5e9;
      color: #388e3c;
    }
    
    .status-abandoned {
      background: #fce4ec;
      color: #c2185b;
    }
    
    .date {
      font-size: 14px;
      color: #999;
    }
    
    .labels {
      display: flex;
      gap: 8px;
      flex-wrap: wrap;
    }
    
    .label {
      padding: 2px 8px;
      border-radius: 4px;
      font-size: 12px;
      color: white;
    }
    
    .pagination {
      display: flex;
      justify-content: center;
      align-items: center;
      gap: 16px;
      margin-top: 24px;
    }
    
    .pagination button {
      padding: 8px 16px;
      border: 1px solid #ddd;
      border-radius: 4px;
      background: white;
      cursor: pointer;
    }
    
    .pagination button:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
  `]
})
export class TodoListComponent implements OnInit {
  private readonly todoService = inject(TodoService);
  
  // Signals for state management (REQ-PERF-003対応)
  todos = signal<Todo[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  currentPage = signal(1);
  totalPages = signal(1);
  
  ngOnInit(): void {
    this.loadPage(1);
  }
  
  loadPage(page: number): void {
    this.loading.set(true);
    this.error.set(null);
    
    this.todoService.getTodos(page, 20).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.todos.set(response.data.items);
          this.currentPage.set(response.data.page);
          this.totalPages.set(response.data.totalPages);
        }
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Todoの読み込みに失敗しました');
        this.loading.set(false);
        console.error('Failed to load todos:', err);
      }
    });
  }
}
