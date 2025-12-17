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
  labels?: LabelModel[];
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
  labelIds?: string[];
}

export interface UpdateTodoRequest {
  title?: string;
  description?: string;
  isCompleted?: boolean;
  labelIds?: string[];
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

export interface LabelModel {
  id: string;  // UUID
  userId: string;  // UUID
  name: string;
  color: string;  // HEX format (#RRGGBB)
  createdAt: string;
  updatedAt: string;
}

export interface CreateLabelRequest {
  name: string;
  color: string;  // HEX format (#RRGGBB)
}

export interface UpdateLabelRequest {
  name?: string;
  color?: string;  // HEX format (#RRGGBB)
}
