import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LabelModel, CreateLabelRequest, UpdateLabelRequest } from '../models';

@Injectable({
  providedIn: 'root'
})
export class LabelService {
  private http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5001/api/labels';

  /**
   * Get all labels for the current user
   */
  getLabels(): Observable<LabelModel[]> {
    return this.http.get<LabelModel[]>(this.apiUrl);
  }

  /**
   * Get a specific label by ID
   */
  getLabel(id: string): Observable<LabelModel> {
    return this.http.get<LabelModel>(`${this.apiUrl}/${id}`);
  }

  /**
   * Create a new label
   */
  createLabel(request: CreateLabelRequest): Observable<LabelModel> {
    return this.http.post<LabelModel>(this.apiUrl, request);
  }

  /**
   * Update an existing label
   */
  updateLabel(id: string, request: UpdateLabelRequest): Observable<LabelModel> {
    return this.http.put<LabelModel>(`${this.apiUrl}/${id}`, request);
  }

  /**
   * Delete a label
   */
  deleteLabel(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /**
   * Validate HEX color format
   */
  isValidHexColor(color: string): boolean {
    return /^#[0-9A-Fa-f]{6}$/.test(color);
  }

  /**
   * Calculate if text should be light or dark based on background color
   */
  getContrastTextColor(hexColor: string): string {
    // Remove # if present
    const color = hexColor.replace('#', '');
    
    // Convert to RGB
    const r = parseInt(color.substring(0, 2), 16);
    const g = parseInt(color.substring(2, 4), 16);
    const b = parseInt(color.substring(4, 6), 16);
    
    // Calculate luminance
    const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
    
    // Return white text for dark backgrounds, black for light backgrounds
    return luminance > 0.5 ? '#000000' : '#FFFFFF';
  }
}
