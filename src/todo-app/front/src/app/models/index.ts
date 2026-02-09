/**
 * Todo モデル
 * REQ-FUNC-001～007対応
 */
export interface Todo {
  todoId: number;
  title: string;
  content?: string;
  status: TodoStatus;
  createdAt: Date;
  updatedAt: Date;
  labels: Label[];
}

/**
 * ステータス enum
 * REQ-FUNC-006対応
 */
export enum TodoStatus {
  NotStarted = 'NotStarted',
  InProgress = 'InProgress',
  Completed = 'Completed',
  Abandoned = 'Abandoned'
}

/**
 * Label モデル
 * REQ-FUNC-008～012対応
 */
export interface Label {
  labelId: number;
  name: string;
  color: string;
  createdAt: Date;
}

/**
 * Todo作成リクエスト
 */
export interface CreateTodoRequest {
  title: string;
  content?: string;
  status: string;
  labelIds: number[];
}

/**
 * Todo更新リクエスト
 */
export interface UpdateTodoRequest {
  title: string;
  content?: string;
  status: string;
  labelIds: number[];
}

/**
 * ページネーション結果
 */
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

/**
 * API レスポンス
 */
export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  errors: ApiError[];
  meta?: ResponseMeta;
}

export interface ApiError {
  code: string;
  message: string;
  field?: string;
}

export interface ResponseMeta {
  timestamp: Date;
  requestId?: string;
  total?: number;
  page?: number;
  pageSize?: number;
}

/**
 * フィルタ条件
 * REQ-FUNC-013～017対応
 */
export interface FilterCriteria {
  keyword?: string;
  statuses?: string[];
  labelIds?: number[];
  startDate?: Date;
  endDate?: Date;
}
