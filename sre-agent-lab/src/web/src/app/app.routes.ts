import { Routes } from '@angular/router';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './services/auth.service';

const authGuard = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/login']);
};

const guestGuard = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/todos']);
};

export const routes: Routes = [
  { path: '', redirectTo: '/todos', pathMatch: 'full' },
  {
    path: 'login',
    loadComponent: () => import('./components/login/login.component').then(m => m.LoginComponent),
    canActivate: [guestGuard]
  },
  {
    path: 'register',
    loadComponent: () => import('./components/register/register.component').then(m => m.RegisterComponent),
    canActivate: [guestGuard]
  },
  {
    path: 'todos',
    loadComponent: () => import('./components/todo-list/todo-list.component').then(m => m.TodoListComponent),
    canActivate: [authGuard]
  },
  {
    path: 'todos/new',
    loadComponent: () => import('./components/todo-create/todo-create.component').then(m => m.TodoCreateComponent),
    canActivate: [authGuard]
  },
  {
    path: 'todos/:id',
    loadComponent: () => import('./components/todo-detail/todo-detail.component').then(m => m.TodoDetailComponent),
    canActivate: [authGuard]
  },
  { path: '**', redirectTo: '/todos' }
];
