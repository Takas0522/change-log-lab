import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { Label, ApiResponse } from '../models';

/**
 * ラベル サービス
 * REQ-FUNC-008～012対応
 */
@Injectable({
  providedIn: 'root'
})
export class LabelService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/v1/labels`;

  /**
   * ラベル一覧取得
   * REQ-FUNC-009対応
   */
  getLabels(): Observable<ApiResponse<Label[]>> {
    return this.http.get<ApiResponse<Label[]>>(this.apiUrl);
  }

  /**
   * ラベル詳細取得
   */
  getLabelById(id: number): Observable<ApiResponse<Label>> {
    return this.http.get<ApiResponse<Label>>(`${this.apiUrl}/${id}`);
  }

  /**
   * ラベル作成
   * REQ-FUNC-010対応
   */
  createLabel(name: string, color: string): Observable<ApiResponse<Label>> {
    return this.http.post<ApiResponse<Label>>(this.apiUrl, { name, color });
  }

  /**
   * ラベル更新
   * REQ-FUNC-011対応
   */
  updateLabel(id: number, name: string, color: string): Observable<ApiResponse<Label>> {
    return this.http.put<ApiResponse<Label>>(`${this.apiUrl}/${id}`, { name, color });
  }

  /**
   * ラベル削除（論理削除）
   * REQ-FUNC-012対応
   */
  deleteLabel(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
