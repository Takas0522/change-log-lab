export interface User {
  id: number;
  email: string;
  displayName: string;
}

export interface LoginRequest {
  email: string;
  password: string;
  deviceId: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  displayName: string;
  deviceId: string;
}

export interface AuthResponse {
  token: string;
  user: User;
}

export interface ListModel {
  id: string;  // UUID
  title: string;
  description?: string;
  ownerId: string;  // UUID
  userRole: 'owner' | 'editor' | 'viewer';
  createdAt: string;
  updatedAt: string;
}

export interface TodoModel {
  id: string;  // UUID
  listId: string;  // UUID
  title: string;
  description?: string;
  isCompleted: boolean;
  dueDate?: string;
  position: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateListRequest {
  title: string;
  description?: string;
}

export interface UpdateListRequest {
  title?: string;
  description?: string;
}

export interface CreateTodoRequest {
  title: string;
  description?: string;
  dueDate?: string;
  position?: number;
}

export interface UpdateTodoRequest {
  title?: string;
  description?: string;
  isCompleted?: boolean;
}

export interface InviteRequest {
  inviteeUserId: number;
}

export interface UserProfile {
  id: number;
  email: string;
  displayName: string;
  createdAt: string;
}
