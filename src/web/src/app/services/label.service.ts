import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { LabelModel, CreateLabelRequest, UpdateLabelRequest, AssignLabelRequest } from '../models';

@Injectable({
  providedIn: 'root'
})
export class LabelService {
  private http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  /**
   * Get all labels for a specific list
   */
  getLabels(listId: string): Observable<LabelModel[]> {
    return this.http.get<LabelModel[]>(`${this.apiUrl}/lists/${listId}/labels`);
  }

  /**
   * Get a specific label
   */
  getLabel(listId: string, labelId: string): Observable<LabelModel> {
    return this.http.get<LabelModel>(`${this.apiUrl}/lists/${listId}/labels/${labelId}`);
  }

  /**
   * Create a new label
   */
  createLabel(listId: string, request: CreateLabelRequest): Observable<LabelModel> {
    return this.http.post<LabelModel>(`${this.apiUrl}/lists/${listId}/labels`, request);
  }

  /**
   * Update an existing label
   */
  updateLabel(listId: string, labelId: string, request: UpdateLabelRequest): Observable<LabelModel> {
    return this.http.put<LabelModel>(`${this.apiUrl}/lists/${listId}/labels/${labelId}`, request);
  }

  /**
   * Delete a label
   */
  deleteLabel(listId: string, labelId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/lists/${listId}/labels/${labelId}`);
  }

  /**
   * Assign a label to a todo
   */
  assignLabelToTodo(listId: string, todoId: string, request: AssignLabelRequest): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/lists/${listId}/todos/${todoId}/labels`, request);
  }

  /**
   * Remove a label from a todo
   */
  removeLabelFromTodo(listId: string, todoId: string, labelId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/lists/${listId}/todos/${todoId}/labels/${labelId}`);
  }
}
