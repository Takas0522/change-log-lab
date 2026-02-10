export interface User {
  userId: string;
  email: string;
  displayName?: string;
}

export interface AuthResponse {
  token: string;
  userId: string;
  email: string;
  displayName?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  displayName?: string;
}

export interface TodoItem {
  id: string;
  title: string;
  body: string | null;
  status: '未着手' | '着手中' | '完了';
  createdAt: string;
  dueDate: string | null;
  completedAt: string | null;
  updatedAt: string;
}

export interface CreateTodoRequest {
  title: string;
  body?: string;
  dueDate?: string;
}

export interface UpdateTodoRequest {
  title?: string;
  body?: string;
  status?: string;
  dueDate?: string;
}

export interface TodoFilter {
  status?: string;
  title?: string;
  dueDateFrom?: string;
  dueDateTo?: string;
}
