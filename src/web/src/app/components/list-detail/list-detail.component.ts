import { Component, OnInit, OnDestroy, signal, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ListService } from '../../services/list.service';
import { TodoService } from '../../services/todo.service';
import { UserService } from '../../services/user.service';
// import { RealtimeService } from '../../services/realtime.service';
import { ListModel, CreateTodoRequest, UserProfile } from '../../models';

@Component({
  selector: 'app-list-detail',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="min-h-screen bg-gray-50">
      <nav class="bg-white shadow-sm">
        <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div class="flex justify-between h-16">
            <div class="flex items-center space-x-4">
              <a routerLink="/lists" class="text-blue-600 hover:text-blue-800">
                ← Back to Lists
              </a>
              @if (list()) {
                <h1 class="text-xl font-bold">{{ list()!.title }}</h1>
                <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
                      [class.bg-blue-100]="list()!.userRole === 'owner'"
                      [class.text-blue-800]="list()!.userRole === 'owner'"
                      [class.bg-gray-100]="list()!.userRole === 'viewer'"
                      [class.text-gray-800]="list()!.userRole === 'viewer'">
                  {{ list()!.userRole }}
                </span>
              }
            </div>
          </div>
        </div>
      </nav>

      <main class="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        <div class="px-4 py-6 sm:px-0">
          @if (list()?.userRole === 'owner') {
            <div class="mb-6 flex space-x-3">
              <button
                (click)="showTodoForm.set(!showTodoForm())"
                class="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700"
              >
                + New Todo
              </button>
              <button
                (click)="showInviteForm.set(!showInviteForm())"
                class="bg-green-600 text-white px-4 py-2 rounded-md hover:bg-green-700"
              >
                Invite User
              </button>
            </div>
          }

          @if (showTodoForm()) {
            <div class="bg-white p-6 rounded-lg shadow mb-6">
              <h3 class="text-lg font-medium mb-4">Create New Todo</h3>
              <form (ngSubmit)="createTodo()">
                <div class="space-y-4">
                  <div>
                    <label for="title" class="block text-sm font-medium text-gray-700">
                      Title
                    </label>
                    <input
                      id="title"
                      type="text"
                      [(ngModel)]="newTodoTitle"
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
                      [(ngModel)]="newTodoDescription"
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
                      (click)="cancelTodoCreate()"
                      class="bg-gray-200 text-gray-700 px-4 py-2 rounded-md hover:bg-gray-300"
                    >
                      Cancel
                    </button>
                  </div>
                </div>
              </form>
            </div>
          }

          @if (showInviteForm()) {
            <div class="bg-white p-6 rounded-lg shadow mb-6">
              <h3 class="text-lg font-medium mb-4">Invite User</h3>
              <form (ngSubmit)="inviteUser()">
                <div class="space-y-4">
                  <div>
                    <label for="search" class="block text-sm font-medium text-gray-700">
                      Search Users
                    </label>
                    <input
                      id="search"
                      type="text"
                      [(ngModel)]="searchQuery"
                      (input)="searchUsers()"
                      name="search"
                      class="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm px-3 py-2 border"
                      placeholder="Search by email or name"
                    />
                  </div>
                  @if (searchResults().length > 0) {
                    <div class="border rounded-md p-2">
                      @for (user of searchResults(); track user.id) {
                        <button
                          type="button"
                          (click)="selectUser(user)"
                          class="w-full text-left px-3 py-2 hover:bg-gray-50 rounded"
                          [class.bg-blue-50]="selectedUser()?.id === user.id"
                        >
                          <div class="font-medium">{{ user.displayName }}</div>
                          <div class="text-sm text-gray-500">{{ user.email }}</div>
                        </button>
                      }
                    </div>
                  }
                  <div class="flex space-x-3">
                    <button
                      type="submit"
                      [disabled]="!selectedUser()"
                      class="bg-green-600 text-white px-4 py-2 rounded-md hover:bg-green-700 disabled:opacity-50"
                    >
                      Send Invite
                    </button>
                    <button
                      type="button"
                      (click)="cancelInvite()"
                      class="bg-gray-200 text-gray-700 px-4 py-2 rounded-md hover:bg-gray-300"
                    >
                      Cancel
                    </button>
                  </div>
                </div>
              </form>
            </div>
          }

          @if (todoLoading()) {
            <div class="text-center py-12">
              <p class="text-gray-500">Loading todos...</p>
            </div>
          } @else if (todos().length === 0) {
            <div class="text-center py-12">
              <p class="text-gray-500">No todos yet. Create your first todo!</p>
            </div>
          } @else {
            <div class="bg-white shadow overflow-hidden sm:rounded-md">
              <ul class="divide-y divide-gray-200">
                @for (todo of todos(); track todo.id) {
                  <li class="px-4 py-4 sm:px-6 hover:bg-gray-50">
                    <div class="flex items-center justify-between">
                      <div class="flex items-center flex-1">
                        <input
                          type="checkbox"
                          [checked]="todo.isCompleted"
                          (change)="toggleTodo(todo)"
                          [disabled]="list()?.userRole === 'viewer'"
                          class="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                        />
                        <div class="ml-3 flex-1">
                          <p class="text-sm font-medium"
                             [class.line-through]="todo.isCompleted"
                             [class.text-gray-500]="todo.isCompleted">
                            {{ todo.title }}
                          </p>
                          @if (todo.description) {
                            <p class="text-sm text-gray-500 mt-1">{{ todo.description }}</p>
                          }
                        </div>
                      </div>
                      @if (list()?.userRole === 'owner') {
                        <button
                          (click)="deleteTodo(todo.id)"
                          class="ml-4 text-sm text-red-600 hover:text-red-900"
                        >
                          Delete
                        </button>
                      }
                    </div>
                  </li>
                }
              </ul>
            </div>
          }
        </div>
      </main>
    </div>
  `
})
export class ListDetailComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly listService = inject(ListService);
  private readonly todoService = inject(TodoService);
  private readonly userService = inject(UserService);
  // private readonly realtimeService = inject(RealtimeService);
  
  readonly list = signal<ListModel | null>(null);
  readonly todos = this.todoService.todos;
  readonly todoLoading = this.todoService.loading;
  readonly showTodoForm = signal(false);
  readonly showInviteForm = signal(false);
  readonly searchResults = signal<UserProfile[]>([]);
  readonly selectedUser = signal<UserProfile | null>(null);
  
  newTodoTitle = '';
  newTodoDescription = '';
  searchQuery = '';
  listId = '';

  ngOnInit() {
    this.listId = this.route.snapshot.paramMap.get('id') || '';
    this.loadList();
    this.loadTodos();
    // this.realtimeService.subscribeToList(this.listId);
  }

  ngOnDestroy() {
    // this.realtimeService.unsubscribeFromList(this.listId);
  }

  loadList() {
    this.listService.getList(this.listId).subscribe({
      next: (list) => this.list.set(list),
      error: (err) => {
        console.error('Failed to load list:', err);
        this.router.navigate(['/lists']);
      }
    });
  }

  loadTodos() {
    this.todoService.loadTodos(this.listId).subscribe();
  }

  createTodo() {
    if (!this.newTodoTitle) return;

    const request: CreateTodoRequest = {
      title: this.newTodoTitle,
      description: this.newTodoDescription || undefined
    };

    this.todoService.createTodo(this.listId, request).subscribe({
      next: () => {
        this.cancelTodoCreate();
      },
      error: (err) => {
        console.error('Failed to create todo:', err);
        alert('Failed to create todo');
      }
    });
  }

  cancelTodoCreate() {
    this.showTodoForm.set(false);
    this.newTodoTitle = '';
    this.newTodoDescription = '';
  }

  toggleTodo(todo: any) {
    this.todoService.toggleComplete(this.listId, todo).subscribe({
      error: (err) => {
        console.error('Failed to update todo:', err);
        alert('Failed to update todo');
      }
    });
  }

  deleteTodo(todoId: string) {
    if (!confirm('Are you sure you want to delete this todo?')) return;

    this.todoService.deleteTodo(this.listId, todoId).subscribe({
      error: (err) => {
        console.error('Failed to delete todo:', err);
        alert('Failed to delete todo');
      }
    });
  }

  searchUsers() {
    if (this.searchQuery.length < 2) {
      this.searchResults.set([]);
      return;
    }

    this.userService.searchUsers(this.searchQuery).subscribe({
      next: (users) => this.searchResults.set(users),
      error: (err) => console.error('Failed to search users:', err)
    });
  }

  selectUser(user: UserProfile) {
    this.selectedUser.set(user);
  }

  inviteUser() {
    const user = this.selectedUser();
    if (!user) return;

    this.listService.inviteUser(this.listId, { inviteeUserId: user.id }).subscribe({
      next: () => {
        alert('Invitation sent successfully!');
        this.cancelInvite();
      },
      error: (err) => {
        console.error('Failed to invite user:', err);
        alert('Failed to send invitation');
      }
    });
  }

  cancelInvite() {
    this.showInviteForm.set(false);
    this.searchQuery = '';
    this.searchResults.set([]);
    this.selectedUser.set(null);
  }
}
