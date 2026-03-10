import { Injectable, signal, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { tap, catchError } from 'rxjs';
import { TodoModel, CreateTodoRequest, UpdateTodoRequest, TodoFilterOptions } from '../models';

@Injectable({
  providedIn: 'root'
})
export class TodoService {
  private readonly http = inject(HttpClient);
  private readonly API_URL = 'http://localhost:5000/api/lists';
  
  private readonly todosSignal = signal<TodoModel[]>([]);
  private readonly loadingSignal = signal<boolean>(false);
  
  readonly todos = this.todosSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();

  loadTodos(listId: string, filters?: TodoFilterOptions) {
    this.loadingSignal.set(true);
    
    let params = new HttpParams();
    if (filters) {
      if (filters.status) {
        params = params.set('status', filters.status);
      }
      if (filters.labelId) {
        params = params.set('labelId', filters.labelId);
      }
      if (filters.search) {
        params = params.set('search', filters.search);
      }
      if (filters.dueDateFrom) {
        params = params.set('dueDateFrom', filters.dueDateFrom);
      }
      if (filters.dueDateTo) {
        params = params.set('dueDateTo', filters.dueDateTo);
      }
    }
    
    return this.http.get<TodoModel[]>(`${this.API_URL}/${listId}/todos`, { params }).pipe(
      tap(todos => {
        this.todosSignal.set(todos);
        this.loadingSignal.set(false);
      }),
      catchError(error => {
        console.error('Failed to load todos:', error);
        this.loadingSignal.set(false);
        throw error;
      })
    );
  }

  getTodo(listId: string, id: string) {
    return this.http.get<TodoModel>(`${this.API_URL}/${listId}/todos/${id}`);
  }

  createTodo(listId: string, request: CreateTodoRequest) {
    return this.http.post<TodoModel>(`${this.API_URL}/${listId}/todos`, request).pipe(
      tap(() => this.loadTodos(listId).subscribe())
    );
  }

  updateTodo(listId: string, id: string, request: UpdateTodoRequest) {
    return this.http.put<TodoModel>(`${this.API_URL}/${listId}/todos/${id}`, request).pipe(
      tap(() => this.loadTodos(listId).subscribe())
    );
  }

  deleteTodo(listId: string, id: string) {
    return this.http.delete(`${this.API_URL}/${listId}/todos/${id}`).pipe(
      tap(() => this.loadTodos(listId).subscribe())
    );
  }

  toggleComplete(listId: string, todo: TodoModel) {
    return this.updateTodo(listId, todo.id, { isCompleted: !todo.isCompleted });
  }
}
