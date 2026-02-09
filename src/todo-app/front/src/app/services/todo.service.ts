import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { Todo, CreateTodoRequest, UpdateTodoRequest, PagedResult, ApiResponse, FilterCriteria } from '../models';

/**
 * Todo サービス
 * REQ-FUNC-001～007対応
 */
@Injectable({
  providedIn: 'root'
})
export class TodoService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/v1/todos`;

  /**
   * Todo一覧取得（ページネーション対応）
   * REQ-FUNC-001対応
   */
  getTodos(
    page: number = 1,
    pageSize: number = 20,
    filter?: FilterCriteria
  ): Observable<ApiResponse<PagedResult<Todo>>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (filter) {
      if (filter.statuses && filter.statuses.length > 0) {
        params = params.set('statuses', filter.statuses.join(','));
      }
      if (filter.labelIds && filter.labelIds.length > 0) {
        params = params.set('labelIds', filter.labelIds.join(','));
      }
      if (filter.keyword) {
        params = params.set('keyword', filter.keyword);
      }
      if (filter.startDate) {
        params = params.set('startDate', filter.startDate.toISOString());
      }
      if (filter.endDate) {
        params = params.set('endDate', filter.endDate.toISOString());
      }
    }

    return this.http.get<ApiResponse<PagedResult<Todo>>>(this.apiUrl, { params });
  }

  /**
   * Todo詳細取得
   * REQ-FUNC-002対応
   */
  getTodoById(id: number): Observable<ApiResponse<Todo>> {
    return this.http.get<ApiResponse<Todo>>(`${this.apiUrl}/${id}`);
  }

  /**
   * Todo作成
   * REQ-FUNC-001対応
   */
  createTodo(request: CreateTodoRequest): Observable<ApiResponse<Todo>> {
    return this.http.post<ApiResponse<Todo>>(this.apiUrl, request);
  }

  /**
   * Todo更新
   * REQ-FUNC-003対応
   */
  updateTodo(id: number, request: UpdateTodoRequest): Observable<ApiResponse<Todo>> {
    return this.http.put<ApiResponse<Todo>>(`${this.apiUrl}/${id}`, request);
  }

  /**
   * Todo削除（論理削除）
   * REQ-FUNC-004対応
   */
  deleteTodo(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /**
   * ステータス更新
   * REQ-FUNC-007対応
   */
  updateStatus(id: number, status: string): Observable<ApiResponse<Todo>> {
    return this.http.patch<ApiResponse<Todo>>(
      `${this.apiUrl}/${id}/status`,
      { status }
    );
  }
}
