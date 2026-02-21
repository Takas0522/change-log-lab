---
description: Angular 21のスタンドアロンコンポーネントをSignalベースで新規作成するプロンプト
mode: agent
tools:
  - read
  - edit
  - search
  - execute
  - todo
---

# Angular コンポーネント作成

Angular 21 のベストプラクティスに従ってスタンドアロンコンポーネントを作成してください。

## 作成依頼

コンポーネント名: ${{component_name}}
機能概要: ${{component_description}}

## Angular 21 必須パターン

### コンポーネント構成

```typescript
@Component({
  selector: 'app-${component_name}',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <!-- ネイティブ制御フロー: @if, @for, @switch を使用 -->
  `
})
export class ${ComponentName}Component {
  // ✅ inject() 関数で DI
  private readonly service = inject(SomeService);
  
  // ✅ Signal ベースの状態管理
  readonly data = signal<Data[]>([]);
  readonly isLoading = signal(false);
  
  // ✅ 派生状態は computed()
  readonly filteredData = computed(() => ...);
  
  // ✅ input()/output() 関数
  readonly inputProp = input.required<string>();
  readonly outputEvent = output<void>();
}
```

### 禁止パターン（使用不可）
- `@NgModule` → スタンドアロンのみ
- `constructor(private svc: Service)` → `inject()` を使用  
- `@Input()` / `@Output()` デコレータ → `input()` / `output()` 関数を使用
- `*ngIf` / `*ngFor` → `@if` / `@for` を使用
- `ngClass` / `ngStyle` → `class` / `style` バインディングを使用

### テンプレート制御フロー

```html
@if (isLoading()) {
  <app-spinner />
} @else {
  @for (item of items(); track item.id) {
    <div>{{ item.name }}</div>
  } @empty {
    <p>データがありません</p>
  }
}
```

## スタイリング

- **Tailwind CSS 4.x** を使用
- レスポンシブ対応（`sm:`, `md:`, `lg:` プレフィックス）

## チェックリスト

- [ ] `ChangeDetectionStrategy.OnPush` を設定
- [ ] Signal ベースの状態管理 (`signal()`, `computed()`)
- [ ] `inject()` による DI
- [ ] ネイティブ制御フロー (`@if`, `@for`, `@switch`)
- [ ] `input()` / `output()` 関数
- [ ] Tailwind CSS でスタイリング
- [ ] ARIA 属性によるアクセシビリティ対応
- [ ] ビルド確認 (`ng build`)
