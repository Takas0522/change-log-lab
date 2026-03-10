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

export interface LabelModel {
  id: string;  // UUID
  listId: string;  // UUID
  name: string;
  color: string;  // Hex color code
  createdAt: string;
  updatedAt: string;
}

export type TodoStatus = 'not_started' | 'in_progress' | 'completed' | 'abandoned';

export interface TodoModel {
  id: string;  // UUID
  listId: string;  // UUID
  title: string;
  description?: string;
  isCompleted: boolean;
  status: TodoStatus;
  dueDate?: string;
  position: number;
  createdAt: string;
  updatedAt: string;
  labels: LabelModel[];
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
  status?: TodoStatus;
  dueDate?: string;
  position?: number;
}

export interface UpdateTodoRequest {
  title?: string;
  description?: string;
  isCompleted?: boolean;
  status?: TodoStatus;
}

export interface CreateLabelRequest {
  name: string;
  color: string;
}

export interface UpdateLabelRequest {
  name?: string;
  color?: string;
}

export interface AssignLabelRequest {
  labelId: string;
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

export interface TodoFilterOptions {
  status?: TodoStatus;
  labelId?: string;
  search?: string;
  dueDateFrom?: string;
  dueDateTo?: string;
}
