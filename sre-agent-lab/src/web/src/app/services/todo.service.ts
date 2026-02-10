import { Injectable, signal, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { tap } from 'rxjs';
import { TodoItem, CreateTodoRequest, UpdateTodoRequest, TodoFilter } from '../models';

@Injectable({
  providedIn: 'root'
})
export class TodoService {
  private readonly http = inject(HttpClient);
  private readonly API_URL = '/api/todos';

  private readonly todosSignal = signal<TodoItem[]>([]);
  readonly todos = this.todosSignal.asReadonly();

  loadTodos(filter?: TodoFilter) {
    let params = new HttpParams();
    if (filter?.status) params = params.set('status', filter.status);
    if (filter?.title) params = params.set('title', filter.title);
    if (filter?.dueDateFrom) params = params.set('dueDateFrom', filter.dueDateFrom);
    if (filter?.dueDateTo) params = params.set('dueDateTo', filter.dueDateTo);

    return this.http.get<TodoItem[]>(this.API_URL, { params }).pipe(
      tap(todos => this.todosSignal.set(todos))
    );
  }

  getTodo(id: string) {
    return this.http.get<TodoItem>(`${this.API_URL}/${id}`);
  }

  createTodo(request: CreateTodoRequest) {
    return this.http.post<TodoItem>(this.API_URL, request);
  }

  updateTodo(id: string, request: UpdateTodoRequest) {
    return this.http.put<TodoItem>(`${this.API_URL}/${id}`, request);
  }

  updateStatus(id: string, status: string) {
    return this.http.patch<TodoItem>(`${this.API_URL}/${id}/status`, { status });
  }

  deleteTodo(id: string) {
    return this.http.delete<void>(`${this.API_URL}/${id}`);
  }

  bomb() {
    return this.http.post<void>(`${this.API_URL}/bomb`, {});
  }
}
