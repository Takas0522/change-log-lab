import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap, catchError } from 'rxjs';
import { TodoModel, CreateTodoRequest, UpdateTodoRequest } from '../models';

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

  loadTodos(listId: string) {
    this.loadingSignal.set(true);
    return this.http.get<TodoModel[]>(`${this.API_URL}/${listId}/todos`).pipe(
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
