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
  
  return router.createUrlTree(['/lists']);
};

export const routes: Routes = [
  { path: '', redirectTo: '/lists', pathMatch: 'full' },
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
    path: 'lists',
    loadComponent: () => import('./components/lists/lists.component').then(m => m.ListsComponent),
    canActivate: [authGuard]
  },
  {
    path: 'lists/:id',
    loadComponent: () => import('./components/list-detail/list-detail.component').then(m => m.ListDetailComponent),
    canActivate: [authGuard]
  },
  {
    path: 'labels',
    loadComponent: () => import('./components/labels/labels.component').then(m => m.LabelsComponent),
    canActivate: [authGuard]
  },
  { path: '**', redirectTo: '/lists' }
];
