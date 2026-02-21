---
applyTo: "src/web/**"
---

# Angular フロントエンド開発ガイドライン（Angular 21）

## 技術スタック

| 技術 | バージョン | 用途 |
|------|-----------|------|
| Angular | 21.0.0 | SPA フレームワーク |
| TypeScript | 5.9.x | 言語 |
| RxJS | 7.8.x | リアクティブプログラミング |
| Tailwind CSS | 4.x | スタイリング |
| @microsoft/signalr | 10.0.0 | リアルタイム通信 |
| Vitest | 4.x | ユニットテスト |

## コンポーネント設計

### スタンドアロンコンポーネント（必須）
Angular 21 ではスタンドアロンコンポーネントがデフォルトです。**NgModules は使用しません**。

```typescript
// ✅ Angular 21 のコンポーネント
@Component({
  selector: 'app-todo-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (isLoading()) {
      <app-spinner />
    } @else {
      @for (todo of todos(); track todo.id) {
        <app-todo-item [todo]="todo" (toggle)="onToggle($event)" />
      }
    }
  `
})
export class TodoListComponent {
  private readonly todoService = inject(TodoService);
  
  todos = signal<Todo[]>([]);
  isLoading = signal(false);
  completedCount = computed(() => this.todos().filter(t => t.isCompleted).length);
}
```

### 禁止パターン
```typescript
// ❌ NgModules は使用しない
@NgModule({ ... })

// ❌ コンストラクタインジェクションは使用しない
constructor(private todoService: TodoService) {}

// ❌ @Input/@Output デコレータは使用しない
@Input() todo!: Todo;
@Output() toggle = new EventEmitter<string>();

// ❌ ngIf/ngFor は使用しない
*ngIf="isLoading"
*ngFor="let todo of todos"
```

### 推奨パターン
```typescript
// ✅ inject() 関数で DI
private readonly todoService = inject(TodoService);

// ✅ input()/output() 関数
todo = input.required<Todo>();
toggle = output<string>();

// ✅ ネイティブ制御フロー
@if (condition) { ... }
@for (item of items; track item.id) { ... }
@switch (value) { @case ('a') { ... } }
```

## 状態管理

### Signal ベース
- ローカル状態: `signal()` を使用
- 派生状態: `computed()` を使用
- 状態更新: `set()` または `update()` を使用（`mutate` は非推奨）

```typescript
// ✅ Signal ベースの状態管理
readonly todos = signal<Todo[]>([]);
readonly filter = signal<'all' | 'active' | 'completed'>('all');
readonly filteredTodos = computed(() => {
  const filter = this.filter();
  return filter === 'all' 
    ? this.todos()
    : this.todos().filter(t => t.isCompleted === (filter === 'completed'));
});
```

## サービス設計

```typescript
// ✅ シングルトンサービス
@Injectable({ providedIn: 'root' })
export class TodoService {
  private readonly http = inject(HttpClient);
  
  getTodos(): Observable<Todo[]> {
    return this.http.get<Todo[]>('/api/todos');
  }
}
```

## ルーティング

- **遅延ロード**: 機能ルートには `loadComponent` を使用
- **関数ガード**: `authGuard`, `guestGuard` を関数として定義

```typescript
export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./login.component'), canActivate: [guestGuard] },
  { path: 'todos', loadComponent: () => import('./todos.component'), canActivate: [authGuard] },
];
```

## HTTP インターセプタ

関数型インターセプタを使用してBFFにリクエストを集約：

```typescript
// ✅ 関数型 HTTP インターセプタ
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(AuthService).getToken();
  if (token) {
    req = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  }
  return next(req);
};
```

## テスト（Vitest）

- フレームワーク: Vitest（Karma/Jasmine の代替）
- コンポーネントテスト: `TestBed` を使用
- 非同期テスト: `fakeAsync` / `waitForAsync` で完了を待つ

## アクセシビリティ

- **AXE チェック** 全パス必須
- **WCAG AA 準拠**: フォーカス管理、カラーコントラスト、ARIA属性
- すべての静的画像に `NgOptimizedImage` を使用
