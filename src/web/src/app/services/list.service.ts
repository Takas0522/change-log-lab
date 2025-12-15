import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap, catchError } from 'rxjs';
import { ListModel, CreateListRequest, UpdateListRequest, InviteRequest } from '../models';

@Injectable({
  providedIn: 'root'
})
export class ListService {
  private readonly http = inject(HttpClient);
  private readonly API_URL = 'http://localhost:5000/api/lists';
  
  private readonly listsSignal = signal<ListModel[]>([]);
  private readonly loadingSignal = signal<boolean>(false);
  
  readonly lists = this.listsSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();

  loadLists() {
    this.loadingSignal.set(true);
    return this.http.get<ListModel[]>(this.API_URL).pipe(
      tap(lists => {
        this.listsSignal.set(lists);
        this.loadingSignal.set(false);
      }),
      catchError(error => {
        console.error('Failed to load lists:', error);
        this.loadingSignal.set(false);
        throw error;
      })
    );
  }

  getList(id: string) {
    return this.http.get<ListModel>(`${this.API_URL}/${id}`);
  }

  createList(request: CreateListRequest) {
    return this.http.post<ListModel>(this.API_URL, request).pipe(
      tap(() => this.loadLists().subscribe())
    );
  }

  updateList(id: string, request: UpdateListRequest) {
    return this.http.put<ListModel>(`${this.API_URL}/${id}`, request).pipe(
      tap(() => this.loadLists().subscribe())
    );
  }

  deleteList(id: string) {
    return this.http.delete(`${this.API_URL}/${id}`).pipe(
      tap(() => this.loadLists().subscribe())
    );
  }

  inviteUser(listId: string, request: InviteRequest) {
    return this.http.post(`${this.API_URL}/${listId}/invite`, request);
  }

  acceptInvite(listId: string) {
    return this.http.post(`${this.API_URL}/${listId}/accept`, {}).pipe(
      tap(() => this.loadLists().subscribe())
    );
  }
}
