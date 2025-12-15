import { Component, OnInit, OnDestroy, signal, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { ListService } from '../../services/list.service';
// import { RealtimeService } from '../../services/realtime.service';
import { CreateListRequest } from '../../models';

@Component({
  selector: 'app-lists',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="min-h-screen bg-gray-50">
      <nav class="bg-white shadow-sm">
        <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div class="flex justify-between h-16">
            <div class="flex items-center">
              <h1 class="text-xl font-bold">Todo Lists</h1>
            </div>
            <div class="flex items-center space-x-4">
              <span class="text-gray-700">{{ user()?.displayName }}</span>
              <button
                (click)="logout()"
                class="text-sm text-gray-600 hover:text-gray-900"
              >
                Logout
              </button>
            </div>
          </div>
        </div>
      </nav>

      <main class="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        <div class="px-4 py-6 sm:px-0">
          <div class="mb-6">
            <button
              (click)="showCreateForm.set(!showCreateForm())"
              class="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700"
            >
              + New List
            </button>
          </div>

          @if (showCreateForm()) {
            <div class="bg-white p-6 rounded-lg shadow mb-6">
              <h3 class="text-lg font-medium mb-4">Create New List</h3>
              <form (ngSubmit)="createList()">
                <div class="space-y-4">
                  <div>
                    <label for="title" class="block text-sm font-medium text-gray-700">
                      Title
                    </label>
                    <input
                      id="title"
                      type="text"
                      [(ngModel)]="newListName"
                      name="title"
                      required
                      class="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm px-3 py-2 border"
                    />
                  </div>
                  <div>
                    <label for="description" class="block text-sm font-medium text-gray-700">
                      Description
                    </label>
                    <textarea
                      id="description"
                      [(ngModel)]="newListDescription"
                      name="description"
                      rows="3"
                      class="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm px-3 py-2 border"
                    ></textarea>
                  </div>
                  <div class="flex space-x-3">
                    <button
                      type="submit"
                      class="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700"
                    >
                      Create
                    </button>
                    <button
                      type="button"
                      (click)="cancelCreate()"
                      class="bg-gray-200 text-gray-700 px-4 py-2 rounded-md hover:bg-gray-300"
                    >
                      Cancel
                    </button>
                  </div>
                </div>
              </form>
            </div>
          }

          @if (loading()) {
            <div class="text-center py-12">
              <p class="text-gray-500">Loading lists...</p>
            </div>
          } @else if (lists().length === 0) {
            <div class="text-center py-12">
              <p class="text-gray-500">No lists yet. Create your first list!</p>
            </div>
          } @else {
            <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
              @for (list of lists(); track list.id) {
                <div class="bg-white overflow-hidden shadow rounded-lg">
                  <div class="px-4 py-5 sm:p-6">
                    <h3 class="text-lg font-medium text-gray-900">
                      <a [routerLink]="['/lists', list.id]" class="hover:text-blue-600">
                        {{ list.title }}
                      </a>
                    </h3>
                    @if (list.description) {
                      <p class="mt-1 text-sm text-gray-500">{{ list.description }}</p>
                    }
                    <div class="mt-2">
                      <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
                            [class.bg-blue-100]="list.userRole === 'owner'"
                            [class.text-blue-800]="list.userRole === 'owner'"
                            [class.bg-gray-100]="list.userRole === 'viewer'"
                            [class.text-gray-800]="list.userRole === 'viewer'">
                        {{ list.userRole }}
                      </span>
                    </div>
                  </div>
                  @if (list.userRole === 'owner') {
                    <div class="bg-gray-50 px-4 py-4 sm:px-6">
                      <button
                        (click)="deleteList(list.id)"
                        class="text-sm text-red-600 hover:text-red-900"
                      >
                        Delete
                      </button>
                    </div>
                  }
                </div>
              }
            </div>
          }
        </div>
      </main>
    </div>
  `
})
export class ListsComponent implements OnInit, OnDestroy {
  private readonly authService = inject(AuthService);
  private readonly listService = inject(ListService);
  // private readonly realtimeService = inject(RealtimeService);
  
  readonly user = this.authService.user;
  readonly lists = this.listService.lists;
  readonly loading = this.listService.loading;
  readonly showCreateForm = signal(false);
  
  newListName = '';
  newListDescription = '';

  ngOnInit() {
    this.listService.loadLists().subscribe();
    // this.realtimeService.connect();
  }

  ngOnDestroy() {
    // this.realtimeService.disconnect();
  }

  createList() {
    if (!this.newListName) return;

    const request: CreateListRequest = {
      title: this.newListName,
      description: this.newListDescription || undefined
    };

    this.listService.createList(request).subscribe({
      next: () => {
        this.cancelCreate();
      },
      error: (err) => {
        console.error('Failed to create list:', err);
        alert('Failed to create list');
      }
    });
  }

  cancelCreate() {
    this.showCreateForm.set(false);
    this.newListName = '';
    this.newListDescription = '';
  }

  deleteList(id: string) {
    if (!confirm('Are you sure you want to delete this list?')) return;

    this.listService.deleteList(id).subscribe({
      error: (err) => {
        console.error('Failed to delete list:', err);
        alert('Failed to delete list');
      }
    });
  }

  logout() {
    this.authService.logout().subscribe();
  }
}
