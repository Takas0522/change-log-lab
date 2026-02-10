# タグラベル機能 統合仕様書

## 1. はじめに

### 1.1 背景

現在のToDoアプリケーションでは、ToDoアイテムはリスト単位でのみ整理が可能であり、以下の課題が存在する。

- **横断的な分類ができない**: 複数のリストに跨がるToDoを「緊急」「バグ修正」「企画」などの観点で横断的に把握する手段がない
- **視覚的な優先度表現がない**: ToDoの重要度やカテゴリを一目で識別する仕組みがなく、タイトルやdescriptionに頼るしかない
- **フィルタリング・検索性が低い**: リスト内のToDoが増加した際に、特定の属性を持つToDoだけを素早く絞り込む方法がない

### 1.2 目的

本機能は、ToDoアイテムに対してユーザーが自由に定義できる**タグ（分類用キーワード）**を付与し、そのタグに対して**ラベル（色・表示名などの視覚的属性）**を設定できる機能を提供する。これにより、ToDoの分類・可視化・フィルタリングを実現し、個人およびチームのタスク管理効率を向上させる。

### 1.3 スコープ

| 区分 | 内容 |
|------|------|
| **対象範囲** | タグのスコープは**リスト単位**とする。タグはリスト（List）に紐づき、そのリストに属するToDoに対してのみ付与可能 |
| **共有** | タグはリストの共有メンバー全員に可視であり、権限に応じた操作制限を設ける |
| **対象外** | グローバルタグ（アカウント横断タグ）は本フェーズの対象外 |

### 1.4 実現する機能要素

| 機能要素 | 説明 |
|---------|------|
| **タグの作成・管理** | リスト単位でタグ（例: "緊急", "バグ", "デザイン"）を作成・編集・削除できる |
| **ラベル属性の設定** | 各タグに色（HEXカラーコード）と表示名を設定し、視覚的に区別できるようにする |
| **ToDoへのタグ付与** | 個々のToDoアイテムに1つ以上のタグを付与・解除できる |
| **タグによるフィルタリング** | リスト内のToDoをタグで絞り込み表示できる |
| **タグの共有** | リストを共有しているメンバー間でタグ定義が共有される |

---

## 2. ビジネス要件

### 2.1 ユースケース

#### UC-01: プロジェクトマネージャーがタスクを優先度別に分類する

**ペルソナ**: 田中 美咲（35歳）、ITスタートアップのプロジェクトマネージャー。5名のチームを管理しており、1つのリストで20~30件のToDoを運用している。

**シナリオ**: 田中さんは「プロダクトリリース準備」リストを管理している。リスト内のToDoが30件に膨れ上がり、どのタスクから手を付けるべきか一目では判断できなくなった。田中さんは「P0-最優先」（赤）、「P1-重要」（橙）、「P2-通常」（青）のタグを作成し、各ToDoにタグを付与する。これにより、チームメンバーがリストを開いた際に、色分けされたタグで優先度を即座に把握できるようになる。

**期待される結果**: タグの色による視覚的な優先度表示により、チーム全体のタスク優先順位の認識が統一され、重要タスクの見落としが削減される。

**関連するユーザーストーリー**: US-TAG-01, US-LBL-01, US-ASN-01, US-ASN-03
**関連するAPI**: POST /api/lists/{listId}/tags, POST /api/lists/{listId}/todos/{todoId}/tags

---

#### UC-02: チームメンバーが担当カテゴリでフィルタリングする

**ペルソナ**: 佐藤 健太（28歳）、フロントエンドエンジニア。共有リストのeditorとして参加しており、UI関連のタスクのみを集中して確認したい。

**シナリオ**: 佐藤さんは「スプリント#12」リストにeditorとして参加している。リストには「フロントエンド」「バックエンド」「インフラ」「デザイン」の4つのタグが設定されている。佐藤さんは「フロントエンド」タグでフィルタリングし、自分が関与すべきタスクだけを一覧表示する。

**期待される結果**: フィルタリングにより、50件中12件のフロントエンド関連タスクだけが表示され、作業対象の把握にかかる時間が大幅に短縮される。

**関連するユーザーストーリー**: US-FLT-01, US-ASN-03
**関連するAPI**: GET /api/lists/{listId}/todos（タグフィルタリングはフロントエンド実装）

---

#### UC-03: リストオーナーがタグのラベルを統一管理する

**ペルソナ**: 山田 太郎（42歳）、部門マネージャー。部署横断プロジェクトのリストオーナーとして、タグの命名規則と色を統一したい。

**シナリオ**: 山田さんが管理する「Q1経営課題」リストには10名のメンバーが参加している。山田さんはリストオーナーとして、タグの命名規則（「部署名-カテゴリ」形式）と色体系（営業=緑系、開発=青系、管理=灰色系）を事前に定義する。

> **整合性注記（UC-03とUS-PRM-02の関係）**:
> 本ユースケースでは「ownerのみがタグを作成・編集・削除し、editorはToDoへのタグ付与のみ可能」とする運用が想定されている。しかし、**MVP（Phase 1）の権限設計では、editorもタグの作成・編集・削除が可能**である（後述「4. 権限設計」参照）。ownerがeditorのタグ作成権限を制限する機能はUS-PRM-02（Could優先度）として定義されており、**Phase 3で対応予定**である。MVPでは、チーム内の運用ルールにより対応する。

**期待される結果**: タグの命名規則が統一され、リスト全体の整理状態が維持される。

**関連するユーザーストーリー**: US-TAG-01, US-TAG-02, US-TAG-03, US-LBL-01, US-LBL-02, US-PRM-02（Phase 3）
**関連するAPI**: POST/PUT/DELETE /api/lists/{listId}/tags/{tagId}

---

#### UC-04: 個人ユーザーがプライベートな目標管理にタグを活用する

**ペルソナ**: 鈴木 愛（24歳）、新社会人。個人の目標管理としてToDoアプリを使用している。

**シナリオ**: 鈴木さんは「2026年目標」というプライベートリストを所有している。「キャリア」（青）、「健康」（緑）、「学習」（紫）、「趣味」（黄）の4つのタグを作成し、各目標ToDoにタグを付与する。月末のふりかえり時に、特定タグでフィルタリングして進捗を確認する。

**期待される結果**: カテゴリ別の目標進捗を視覚的に把握でき、バランスの取れた目標管理が可能になる。

**関連するユーザーストーリー**: US-TAG-01, US-LBL-01, US-ASN-01, US-FLT-01
**関連するAPI**: POST /api/lists/{listId}/tags, POST /api/lists/{listId}/todos/{todoId}/tags

---

#### UC-05: viewerがタグを参照して進捗を把握する

**ペルソナ**: 伊藤 直人（50歳）、経営幹部。各部門のプロジェクトリストにviewerとして参加し、進捗状況を定期的に確認している。

**シナリオ**: 伊藤さんは複数のプロジェクトリストにviewerとして参加している。各リストにはプロジェクトマネージャーが設定した「完了待ち」「ブロック中」「進行中」のタグが付いている。伊藤さんは「ブロック中」タグでフィルタリングし、対処が必要なタスクを即座に特定する。viewerであるため、タグの変更やToDoの編集はできないが、フィルタリングによる閲覧は自由に行える。

**期待される結果**: 経営層が各プロジェクトのボトルネックを迅速に把握でき、意思決定のスピードが向上する。

**関連するユーザーストーリー**: US-PRM-01, US-FLT-01, US-ASN-03
**関連するAPI**: GET /api/lists/{listId}/tags, GET /api/lists/{listId}/todos

---

#### UC-06: editorが既存ToDoに複数タグを付けて多角的に分類する

**ペルソナ**: 高橋 裕子（32歳）、マーケティング担当。共有リストのeditorとしてキャンペーン管理を行っている。

**シナリオ**: 高橋さんは「夏季キャンペーン施策」リストを管理している。1つのToDo「LP制作」に対して、「デザイン」「外注」「8月締切」の3つのタグを同時に付与する。これにより、「デザイン」タグでフィルタしてもLPの制作進捗が確認でき、「外注」タグでフィルタすれば外注管理の観点でも同じタスクが把握できるようになる。

**期待される結果**: 1つのToDoを複数の切り口で分類・検索でき、多角的なタスク管理が実現する。

**関連するユーザーストーリー**: US-ASN-01, US-ASN-03, US-FLT-01
**関連するAPI**: POST /api/lists/{listId}/todos/{todoId}/tags（複数回呼び出し）

---

### 2.2 ユーザーストーリー

#### タグ管理（CRUD）

| ID | ユーザーストーリー | 優先度 | Phase | 対応API |
|----|-------------------|--------|-------|---------|
| US-TAG-01 | リストのowner/editorとして、リストに新しいタグを作成したい。それにより、ToDoを分類するためのカテゴリを定義できるようにしたい。 | Must | 1 | POST /api/lists/{listId}/tags |
| US-TAG-02 | リストのowner/editorとして、タグの表示名を編集したい。それにより、運用中に名称を修正・改善できるようにしたい。 | Must | 1 | PUT /api/lists/{listId}/tags/{tagId} |
| US-TAG-03 | リストのowner/editorとして、不要になったタグを削除したい。それにより、タグ一覧を整理された状態に保ちたい。 | Must | 1 | DELETE /api/lists/{listId}/tags/{tagId} |
| US-TAG-04 | リストのowner/editorとして、タグ一覧を確認したい。それにより、現在リストで使用可能なタグを把握したい。 | Must | 1 | GET /api/lists/{listId}/tags |

#### ラベル属性管理

| ID | ユーザーストーリー | 優先度 | Phase | 対応API / 実装 |
|----|-------------------|--------|-------|----------------|
| US-LBL-01 | リストのowner/editorとして、タグに色（カラーコード）を設定したい。それにより、タグを視覚的に区別できるようにしたい。 | Must | 1 | POST /api/lists/{listId}/tags（colorフィールド） |
| US-LBL-02 | リストのowner/editorとして、タグの色を変更したい。それにより、チームの運用に合わせて色体系を調整したい。 | Must | 1 | PUT /api/lists/{listId}/tags/{tagId}（colorフィールド） |
| US-LBL-03 | システムとして、タグ作成時にデフォルト色を自動割当したい。それにより、ユーザーが色選択を省略してもタグが視覚的に区別できるようにしたい。 | Should | 2 | DBデフォルト値 '#6B7280'（MVP）。Phase 2でインテリジェントな色ローテーション実装予定 |

#### ToDoへのタグ付与

| ID | ユーザーストーリー | 優先度 | Phase | 対応API |
|----|-------------------|--------|-------|---------|
| US-ASN-01 | リストのowner/editorとして、ToDoに1つ以上のタグを付与したい。それにより、ToDoを複数の観点で分類できるようにしたい。 | Must | 1 | POST /api/lists/{listId}/todos/{todoId}/tags |
| US-ASN-02 | リストのowner/editorとして、ToDoからタグを解除したい。それにより、誤って付与したタグや不要になったタグを外せるようにしたい。 | Must | 1 | DELETE /api/lists/{listId}/todos/{todoId}/tags/{tagId} |
| US-ASN-03 | リストのメンバーとして、ToDoに付与されたタグをToDo一覧上で確認したい。それにより、各ToDoのカテゴリを一目で把握したい。 | Must | 1 | GET /api/lists/{listId}/todos（レスポンスのtagsフィールド） |

#### フィルタリング・表示

| ID | ユーザーストーリー | 優先度 | Phase | 対応API / 実装 |
|----|-------------------|--------|-------|----------------|
| US-FLT-01 | リストのメンバー（owner/editor/viewer）として、特定のタグが付いたToDoだけをフィルタリング表示したい。それにより、関心のあるToDoだけを素早く確認したい。 | Must | 1 | フロントエンド実装（list-detail.component.ts） |
| US-FLT-02 | リストのメンバーとして、複数タグのAND/OR条件でフィルタリングしたい。それにより、より精密な絞り込みを行いたい。 | Could | 2 | フロントエンド実装 |
| US-FLT-03 | リストのメンバーとして、タグが未設定のToDoだけを表示したい。それにより、分類漏れのToDoを発見し整理したい。 | Should | 2 | フロントエンド実装 |

#### 権限・共有

| ID | ユーザーストーリー | 優先度 | Phase | 対応API / 実装 |
|----|-------------------|--------|-------|----------------|
| US-PRM-01 | viewerとして、リストのタグ定義とToDoに付与されたタグを閲覧したい。ただしタグの作成・編集・削除・付与は行えないようにしたい。 | Must | 1 | 権限チェック（TagsController） |
| US-PRM-02 | リストのownerとして、editorにタグの作成権限を許可/制限したい。それにより、タグの乱立を防ぎつつ柔軟な運用を可能にしたい。 | Could | 3 | 未実装（Phase 3対応） |

### 2.3 ビジネス価値

#### ユーザーエンゲージメントの向上

| 指標 | 期待効果 |
|------|---------|
| **DAU（日次アクティブユーザー数）** | タグによる整理・フィルタリングが日常的な操作となり、アプリの利用頻度が向上する |
| **セッション時間** | フィルタリングにより目的のToDoへ素早く到達できるため、ストレスの少ない体験が提供され、継続利用率が向上する |
| **機能利用率** | タグ機能は直感的な操作であり、既存ユーザーの多くが自発的に活用することが見込まれる |

#### 競合優位性の確保

- 主要なToDoアプリケーション（Todoist, Microsoft To Do, TickTickなど）はいずれもタグまたはラベル機能を提供しており、本機能の欠如は**競合に対する明確な劣位**となっている
- 特にチーム利用においてタグによる分類は不可欠な機能であり、法人導入の判断材料となる

#### チームコラボレーションの強化

- 共有リスト内でタグが統一されることで、チームメンバー間の**タスク分類に関する共通認識**が形成される
- viewer権限のステークホルダーがタグフィルタリングで進捗を把握できることにより、**報告コストの削減**に繋がる

#### 拡張性の基盤構築

- タグ機能は将来的な高度機能の基盤となる
  - タグ別の集計・レポート機能
  - タグに基づく自動化ルール（例: 「緊急」タグ付与時に通知）
  - タグベースのダッシュボード・分析
  - リスト横断でのタグ検索

#### 定量的なKPI目標（参考値）

| KPI | 目標値 | 計測時期 |
|-----|--------|---------|
| タグ機能利用率（アクティブユーザーのうちタグを1つ以上作成した割合） | 40%以上 | リリース後3ヶ月 |
| タグ付きToDo率（新規作成ToDoのうちタグが付与された割合） | 30%以上 | リリース後3ヶ月 |
| フィルタリング利用率（週1回以上タグフィルタを使用したユーザーの割合） | 25%以上 | リリース後3ヶ月 |

---

## 3. システム要件

### 3.1 システム構成への影響

#### 影響を受けるサービス

| サービス | パス | 影響内容 |
|----------|------|----------|
| todo-service | `/src/todo-service/api/` | Model・Controller・DTO・DbContextの追加・変更 |
| bff-service | `/src/bff-service/api/` | プロキシコントローラーの追加 |
| web (Angular) | `/src/web/src/app/` | Model・Service・Componentの追加・変更 |

#### 影響を受けないサービス

auth-service, user-service はタグ機能の追加による変更は不要。

### 3.2 データモデル設計

#### 3.2.1 新規テーブル

##### tags テーブル

| カラム名 | 型 | 制約 | 説明 |
|----------|------|------|------|
| id | UUID | PK | タグ一意識別子 |
| list_id | UUID | FK -> lists(id) ON DELETE CASCADE | 所属リストID |
| name | VARCHAR(50) | NOT NULL | タグ名称 |
| color | VARCHAR(7) | NOT NULL, DEFAULT '#6B7280' | 表示色（HEXカラーコード） |
| position | INT | NOT NULL, DEFAULT 0 | 表示順序（Phase 2でUI実装予定） |
| created_at | TIMESTAMP WITH TIME ZONE | NOT NULL, DEFAULT NOW() | 作成日時 |
| updated_at | TIMESTAMP WITH TIME ZONE | NOT NULL, DEFAULT NOW() | 更新日時 |

制約:
- UNIQUE(list_id, name) -- 同一リスト内でタグ名の重複を防止

##### todo_tags テーブル（中間テーブル）

| カラム名 | 型 | 制約 | 説明 |
|----------|------|------|------|
| id | UUID | PK | レコード一意識別子 |
| todo_id | UUID | FK -> todos(id) ON DELETE CASCADE | 対象ToDoのID |
| tag_id | UUID | FK -> tags(id) ON DELETE CASCADE | 付与するタグのID |
| created_at | TIMESTAMP WITH TIME ZONE | NOT NULL, DEFAULT NOW() | 作成日時 |

制約:
- UNIQUE(todo_id, tag_id) -- 同一ToDoに同一タグの重複付与を防止

#### 3.2.2 ER図

```
lists (既存) ──1:N──> tags (新規) ──N:M──> todos (既存)
                                    [中間: todo_tags]
```

- リスト削除時: CASCADE により tags -> todo_tags が連鎖削除される
- ToDo削除時: CASCADE により todo_tags が削除される
- タグ削除時: CASCADE により todo_tags が削除される（ビジネス要件「データ整合性」を満たす）

#### 3.2.3 バックエンド Model クラス

| 区分 | ファイル | 内容 |
|------|---------|------|
| 新規 | Tag.cs | tags テーブルに対応するエンティティ |
| 新規 | TodoTag.cs | todo_tags テーブルに対応するエンティティ |
| 変更 | Todo.cs | `ICollection<TodoTag> TodoTags` ナビゲーションプロパティ追加 |
| 変更 | List.cs | `ICollection<Tag> Tags` ナビゲーションプロパティ追加 |

### 3.3 API設計

#### 3.3.1 タグCRUD API

| メソッド | パス | 権限 | 説明 | 対応US |
|---------|------|------|------|--------|
| GET | /api/lists/{listId}/tags | owner/editor/viewer | リストのタグ一覧取得 | US-TAG-04 |
| POST | /api/lists/{listId}/tags | owner/editor | タグ新規作成 | US-TAG-01, US-LBL-01 |
| PUT | /api/lists/{listId}/tags/{tagId} | owner/editor | タグ更新（名称・色） | US-TAG-02, US-LBL-02 |
| DELETE | /api/lists/{listId}/tags/{tagId} | owner/editor | タグ削除 | US-TAG-03 |

##### POST /api/lists/{listId}/tags リクエスト / レスポンス

リクエスト (CreateTagRequest):
```json
{
  "name": "緊急",
  "color": "#EF4444"
}
```

バリデーション:
- `name`: 必須、1~50文字、同一リスト内で一意
- `color`: 任意（省略時デフォルト `#6B7280`）、HEXカラーコード形式（`#` + 6桁16進数）

レスポンス (TagResponse, 201 Created):
```json
{
  "id": "uuid",
  "listId": "uuid",
  "name": "緊急",
  "color": "#EF4444",
  "position": 0,
  "createdAt": "2026-01-01T00:00:00Z",
  "updatedAt": "2026-01-01T00:00:00Z"
}
```

##### PUT /api/lists/{listId}/tags/{tagId} リクエスト

リクエスト (UpdateTagRequest):
```json
{
  "name": "最優先",
  "color": "#DC2626",
  "position": 1
}
```

バリデーション:
- `name`: 任意（変更時は1~50文字、同一リスト内で一意）
- `color`: 任意、HEXカラーコード形式
- `position`: 任意、整数値

レスポンス (200 OK): TagResponseと同一形式

##### DELETE /api/lists/{listId}/tags/{tagId}

レスポンス: 204 No Content

副作用: `todo_tags` テーブルの該当タグに関する全レコードがCASCADE削除される

#### 3.3.2 ToDoへのタグ付け・解除 API

| メソッド | パス | 権限 | 説明 | 対応US |
|---------|------|------|------|--------|
| GET | /api/lists/{listId}/todos/{todoId}/tags | owner/editor/viewer | ToDoのタグ一覧取得 | US-ASN-03 |
| POST | /api/lists/{listId}/todos/{todoId}/tags | owner/editor | ToDoにタグ付与 | US-ASN-01 |
| DELETE | /api/lists/{listId}/todos/{todoId}/tags/{tagId} | owner/editor | ToDoからタグ解除 | US-ASN-02 |

##### POST /api/lists/{listId}/todos/{todoId}/tags リクエスト

リクエスト (AddTodoTagRequest):
```json
{
  "tagId": "uuid"
}
```

バリデーション:
- `tagId`: 必須、同一リストに属するタグのIDであること
- 同一ToDoに同一タグが既に付与されている場合は `409 Conflict` を返却

レスポンス (TodoTagResponse, 201 Created):
```json
{
  "id": "uuid",
  "todoId": "uuid",
  "tagId": "uuid",
  "tag": {
    "id": "uuid",
    "name": "重要",
    "color": "#EF4444"
  },
  "createdAt": "2026-01-01T00:00:00Z"
}
```

##### DELETE /api/lists/{listId}/todos/{todoId}/tags/{tagId}

レスポンス: 204 No Content

#### 3.3.3 既存API変更

| メソッド | パス | 変更内容 | 対応US |
|---------|------|---------|--------|
| GET | /api/lists/{listId}/todos | レスポンスの各ToDoオブジェクトに `tags` フィールド（TagSummaryResponse[]）を追加 | US-ASN-03 |
| GET | /api/lists/{id} | レスポンスに `tags` フィールド（TagResponse[]）および各todoの `tags` フィールドを追加 | US-TAG-04, US-ASN-03 |

TagSummaryResponse:
```json
{
  "id": "uuid",
  "name": "緊急",
  "color": "#EF4444"
}
```

#### 3.3.4 DTO一覧

| 区分 | DTO名 | 用途 |
|------|-------|------|
| 新規 | CreateTagRequest | タグ作成リクエスト |
| 新規 | UpdateTagRequest | タグ更新リクエスト |
| 新規 | TagResponse | タグ詳細レスポンス |
| 新規 | TagSummaryResponse | ToDo一覧内のタグ概要レスポンス |
| 新規 | AddTodoTagRequest | ToDoタグ付与リクエスト |
| 新規 | TodoTagResponse | ToDoタグ付与レスポンス |
| 変更 | TodoResponse | `tags` フィールド追加 |
| 変更 | ListDetailResponse | `tags` フィールド追加 |

#### 3.3.5 クロスリスト操作の防止

タグ付与時（POST /api/lists/{listId}/todos/{todoId}/tags）に以下のバリデーションを実施する。

- `todoId` が `listId` に属するToDoであること
- `tagId` が `listId` に属するTagであること
- いずれかが不一致の場合は `400 Bad Request` を返却

### 3.4 フロントエンド変更点

#### 3.4.1 モデル追加

| インターフェース | 内容 |
|----------------|------|
| TagModel | タグ情報（id, listId, name, color, position, createdAt, updatedAt） |
| TagSummary | タグ概要（id, name, color） |
| CreateTagRequest | タグ作成リクエスト型 |
| UpdateTagRequest | タグ更新リクエスト型 |

#### 3.4.2 既存モデル変更

- TodoModel に `tags: TagSummary[]` プロパティを追加

#### 3.4.3 サービス追加

- 新規 `tag.service.ts`: タグCRUD + ToDoタグ付与・解除のAPI呼び出し
- 状態管理は既存サービスと同様にAngular Signalsパターン（`signal` / `asReadonly()`）を採用

#### 3.4.4 コンポーネント変更

- `list-detail.component.ts` にタグ管理UI、タグ表示、フィルタリング機能を追加
  - **タグ管理セクション**: リスト詳細画面上部にタグ一覧を色付きチップで表示。owner/editorは「+ タグ追加」ボタンでタグ名・色を指定して作成可能
  - **Todoアイテム内タグ表示**: 各Todoのタイトル下にタグチップ（背景色付き）を表示
  - **タグフィルタリング**: タグチップクリックで単一タグフィルタリング（Phase 1）

#### 3.4.5 色選択UI

プリセットカラーパレット（8色）からの選択方式:
- `#EF4444`(赤), `#F59E0B`(黄), `#10B981`(緑), `#3B82F6`(青)
- `#8B5CF6`(紫), `#EC4899`(ピンク), `#6B7280`(グレー), `#F97316`(オレンジ)

### 3.5 BFFサービス変更点

| 区分 | ファイル | 内容 |
|------|---------|------|
| 新規 | TagsController.cs | todo-serviceのタグCRUD APIへのプロキシ |
| 変更 | TodosController.cs | ToDoタグ付与・解除ルートの追加 |

BFFのプロキシルーティング:

| HTTPメソッド | BFFパス | プロキシ先 (TodoService) |
|-------------|---------|--------------------------|
| GET | /api/lists/{listId}/tags | /api/lists/{listId}/tags |
| POST | /api/lists/{listId}/tags | /api/lists/{listId}/tags |
| PUT | /api/lists/{listId}/tags/{tagId} | /api/lists/{listId}/tags/{tagId} |
| DELETE | /api/lists/{listId}/tags/{tagId} | /api/lists/{listId}/tags/{tagId} |
| GET | /api/lists/{listId}/todos/{todoId}/tags | /api/lists/{listId}/todos/{todoId}/tags |
| POST | /api/lists/{listId}/todos/{todoId}/tags | /api/lists/{listId}/todos/{todoId}/tags |
| DELETE | /api/lists/{listId}/todos/{todoId}/tags/{tagId} | /api/lists/{listId}/todos/{todoId}/tags/{tagId} |

### 3.6 データベースマイグレーション

#### 新規マイグレーション: migration_add_tags.sql

```sql
BEGIN;

-- Tags table
CREATE TABLE IF NOT EXISTS tags (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    list_id UUID NOT NULL REFERENCES lists(id) ON DELETE CASCADE,
    name VARCHAR(50) NOT NULL,
    color VARCHAR(7) NOT NULL DEFAULT '#6B7280',
    position INT NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    UNIQUE(list_id, name)
);

CREATE INDEX IF NOT EXISTS idx_tags_list ON tags(list_id);
CREATE INDEX IF NOT EXISTS idx_tags_list_position ON tags(list_id, position);

-- Todo-Tag junction table
CREATE TABLE IF NOT EXISTS todo_tags (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    todo_id UUID NOT NULL REFERENCES todos(id) ON DELETE CASCADE,
    tag_id UUID NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    UNIQUE(todo_id, tag_id)
);

CREATE INDEX IF NOT EXISTS idx_todo_tags_todo ON todo_tags(todo_id);
CREATE INDEX IF NOT EXISTS idx_todo_tags_tag ON todo_tags(tag_id);

COMMIT;
```

#### DbContext変更

- `DbSet<Tag>` および `DbSet<TodoTag>` の追加
- `OnModelCreating` でリレーション・制約定義
- `UpdateTimestamps` メソッドの拡張（Tag.updated_at の自動更新）

### 3.7 イベント駆動基盤への統合

既存のOutbox + NOTIFY/LISTEN基盤に以下のイベントタイプを追加する。

| イベントタイプ | 発火タイミング | 用途 |
|---------------|--------------|------|
| tag_created | タグ新規作成時 | リアルタイム同期（Phase 2） |
| tag_updated | タグ更新時 | リアルタイム同期（Phase 2） |
| tag_deleted | タグ削除時 | リアルタイム同期（Phase 2） |
| todo_tag_added | ToDoへのタグ付与時 | リアルタイム同期（Phase 2） |
| todo_tag_removed | ToDoからのタグ解除時 | リアルタイム同期（Phase 2） |

> **注記**: イベントはPhase 1でOutboxテーブルに記録する設計とするが、SignalR経由のリアルタイム配信はPhase 2で実装する。

---

## 4. 権限設計

### 4.1 権限マトリクス（Phase 1: MVP）

| 操作 | owner | editor | viewer | 未参加者 |
|------|:-----:|:------:|:------:|:--------:|
| タグ一覧取得 | OK | OK | OK | 拒否（403） |
| タグ作成 | OK | OK | 拒否（403） | 拒否（403） |
| タグ更新（名称・色） | OK | OK | 拒否（403） | 拒否（403） |
| タグ削除 | OK | OK | 拒否（403） | 拒否（403） |
| ToDoのタグ一覧取得 | OK | OK | OK | 拒否（403） |
| ToDoへのタグ付与 | OK | OK | 拒否（403） | 拒否（403） |
| ToDoからのタグ解除 | OK | OK | 拒否（403） | 拒否（403） |
| タグによるフィルタリング（表示） | OK | OK | OK | 拒否（403） |

### 4.2 権限設計の補足事項

- 権限チェックは既存のowner/editor/viewer権限モデルを踏襲する（制約C-02）
- 認証は既存のJWT認証・認可基盤を利用する（前提P-01）
- **Phase 1ではeditorにタグCRUD権限を付与する**。ビジネス要件UC-03で想定されている「ownerのみがタグを管理し、editorのタグ作成権限を制限する」機能はUS-PRM-02（Could優先度）として定義されており、Phase 3で対応する

### 4.3 Phase 3での権限拡張予定（US-PRM-02）

Phase 3では、ownerがeditorのタグ操作権限を個別に制御できる機能を追加予定。

| 操作 | owner | editor（権限あり） | editor（権限なし） | viewer |
|------|:-----:|:-----------------:|:-----------------:|:------:|
| タグ作成 | OK | OK | 拒否 | 拒否 |
| タグ更新 | OK | OK | 拒否 | 拒否 |
| タグ削除 | OK | OK | 拒否 | 拒否 |
| ToDoへのタグ付与 | OK | OK | OK | 拒否 |

> Phase 3の権限拡張時には、list_members テーブルへの `can_manage_tags` フラグ追加、またはタグ管理専用ロールの導入を検討する。

---

## 5. 非機能要件・制約

### 5.1 ビジネス上の制約

| # | 制約事項 | 詳細 | 検証方法 |
|---|---------|------|---------|
| C-01 | **既存データへの影響なし** | タグ機能の追加によって、既存のToDo・リスト・メンバーのデータが破損・変更されないこと | マイグレーション実行前後のデータ比較テスト |
| C-02 | **権限モデルの一貫性** | 既存のowner/editor/viewer権限モデルを踏襲 | 権限チェックの統合テスト |
| C-03 | **マイクロサービス境界の維持** | タグ機能はtodo-service内に実装し、新たなマイクロサービスの追加は行わない | アーキテクチャレビュー |
| C-04 | **パフォーマンス劣化の回避** | 既存のToDo一覧取得APIのレスポンスタイムが有意に劣化しないこと | 負荷テスト（Eager LoadingによるN+1回避で対応） |
| C-05 | **段階的リリース可能な設計** | MVP以降の拡張機能を段階的に追加できるよう、拡張性を考慮した設計とすること | 設計レビュー |

### 5.2 前提条件

| # | 前提条件 | 詳細 |
|---|---------|------|
| P-01 | 認証基盤は既存を利用 | タグ操作のAPIは既存のJWT認証・認可基盤を利用する |
| P-02 | リアルタイム同期への対応 | 既存のSignalR基盤を活用（Phase 2で対応） |
| P-03 | DBはPostgreSQLを利用 | 既存のPostgreSQLインスタンス（todo-db）内にタグ関連テーブルを追加する |
| P-04 | リスト単位のタグスコープ | グローバルタグは本フェーズでは対象外 |

### 5.3 スケーラビリティ上限値

| 項目 | 上限値 | 根拠 |
|------|--------|------|
| 1リストあたりのタグ数 | 50個 | 運用上の管理容易性とUIの表示限界を考慮 |
| 1ToDoあたりのタグ数 | 10個 | 視認性の維持と多角的分類の両立 |

> これらの上限値はAPIのバリデーションレイヤーで制御する。

### 5.4 パフォーマンス要件

| 項目 | 要件 | 実現手段 |
|------|------|---------|
| N+1問題の回避 | ToDo一覧取得時にタグ情報を含むクエリがN+1にならないこと | Eager Loading（Include）の使用 |
| インデックス最適化 | タグ関連の検索・結合が効率的に行えること | list_id, todo_id, tag_id にインデックス作成 |
| 既存API劣化防止 | ToDo一覧取得のレスポンスタイムが200ms以内を維持 | インデックス + Eager Loading |

### 5.5 データ整合性

| 項目 | 要件 | 実現手段 |
|------|------|---------|
| タグ削除時のクリーンアップ | タグ削除時に、当該タグが付与されていた全ToDoとの関連（todo_tags）が適切に削除されること | FK ON DELETE CASCADE |
| リスト削除時の連鎖削除 | リスト削除時に、リストに属する全タグおよびtodo_tagsが削除されること | FK ON DELETE CASCADE |
| ユニーク制約 | 同一リスト内でのタグ名重複、同一ToDoへの同一タグ重複付与を防止 | UNIQUE制約 |

### 5.6 可用性

- タグ機能の障害が既存のToDo CRUD操作に影響を与えないこと
- タグ関連テーブルへのクエリ失敗時に、ToDo一覧の基本表示は維持されること（タグ情報のみ欠落を許容）

### 5.7 アクセシビリティ

- タグの色だけに依存せず、表示名でも識別可能なUI設計とすること（色覚多様性への配慮）
- タグのカラー表示と合わせて、必ずテキストラベルを併記する

### 5.8 デプロイ要件

- フロントエンド（Angular）とバックエンド（todo-service, bff-service）の**同時デプロイが必要**
  - 既存のToDo一覧APIレスポンスにtagsフィールドが追加されるため、フロントエンドが対応していない状態でバックエンドのみ更新すると、未知のフィールドが返却される
  - 逆に、フロントエンドのみ先行デプロイすると、APIから期待するtagsフィールドが返却されず表示エラーの可能性がある
- DBマイグレーション（migration_add_tags.sql）はサービスデプロイ前に実行すること

### 5.9 テスト要件

| テスト種別 | 対象 | 概要 |
|-----------|------|------|
| 単体テスト | TagsController | CRUD操作の正常系・異常系 |
| 単体テスト | TodosController（タグ付与） | タグ付与/解除の正常系・異常系 |
| 統合テスト | API -> DB | EF Core + PostgreSQL経由のCRUD検証 |
| 権限テスト | 全タグAPI | owner/editor/viewerそれぞれの操作可否検証 |
| クロスリストテスト | タグ付与API | 別リストのタグ/Todoへの不正操作防止検証 |
| E2Eテスト | フロントエンド | タグ作成 -> Todoへの付与 -> フィルタリングの一連の操作 |

---

## 6. リリース計画

### 6.1 Phase 1: MVP

**目標**: タグの基本的な作成・管理・付与・表示を実現し、ユーザーがToDoを視覚的に分類できる状態にする。

| 含まれるユーザーストーリー | 概要 |
|--------------------------|------|
| US-TAG-01~04 | タグCRUD |
| US-LBL-01~02 | タグ色の設定・変更 |
| US-ASN-01~03 | ToDoへのタグ付与・解除・表示 |
| US-FLT-01 | 単一タグによるフィルタリング |
| US-PRM-01 | viewer閲覧権限 |

**MVPに含まれるシステム実装**:
- tags, todo_tags テーブル作成 + インデックス
- TagsController, DTO, Model追加
- 既存API（TodoResponse, ListDetailResponse）へのtagsフィールド追加
- BFFプロキシ追加
- フロントエンド: タグ管理UI, タグ表示, 単一タグフィルタリング
- イベント（Outbox記録のみ、リアルタイム配信はPhase 2）

**権限**: owner/editorがタグCRUD可能、viewerは閲覧のみ

**MVP完了条件（Definition of Done）**:
- リストのowner/editorがタグを作成し、色と表示名を設定できる
- ToDoにタグを付与・解除でき、一覧画面でタグがラベルとして色付き表示される
- 単一タグによるフィルタリングが動作する
- viewerはタグの閲覧のみ可能で、変更操作は拒否される
- 既存のToDo CRUD機能に影響がない

### 6.2 Phase 2: フィルタリング強化と利便性向上

| 含まれるユーザーストーリー | 概要 |
|--------------------------|------|
| US-LBL-03 | デフォルト色のインテリジェントな自動割当 |
| US-FLT-02 | 複数タグのAND/OR条件フィルタリング |
| US-FLT-03 | タグ未設定ToDoの表示 |

**追加実装**:
- タグの並び順変更（positionフィールド活用）
- SignalR経由のリアルタイムタグ同期
- インテリジェントなデフォルト色ローテーション

### 6.3 Phase 3: 高度機能と拡張

| 含まれるユーザーストーリー | 概要 |
|--------------------------|------|
| US-PRM-02 | editorのタグ作成権限の許可/制限 |

**追加実装**:
- タグ別ダッシュボード
- リスト横断検索
- タグに基づく自動通知ルール
- タグテンプレート

### 6.4 リリース判断基準

| Phase | リリース判断基準 |
|-------|---------------|
| Phase 1 (MVP) | MVP完了条件を満たし、既存機能のリグレッションテストに合格すること |
| Phase 2 | Phase 1リリース後のユーザーフィードバックを反映し、タグ機能利用率が20%以上であること |
| Phase 3 | Phase 2までの機能が安定稼働しており、ビジネス上の優先度が他の新規機能より高いと判断されること |

---

## 7. 変更影響範囲

### 7.1 新規ファイル一覧

| # | ファイルパス | 内容 |
|---|------------|------|
| 1 | `/src/todo-service/api/Models/Tag.cs` | タグエンティティ |
| 2 | `/src/todo-service/api/Models/TodoTag.cs` | ToDoタグ中間エンティティ |
| 3 | `/src/todo-service/api/Controllers/TagsController.cs` | タグCRUD + ToDoタグ付与APIコントローラー |
| 4 | `/src/todo-service/db/migrations/migration_add_tags.sql` | タグ関連テーブル作成マイグレーション |
| 5 | `/src/bff-service/api/Controllers/TagsController.cs` | BFFプロキシコントローラー |
| 6 | `/src/web/src/app/services/tag.service.ts` | タグAPI呼び出しサービス |

### 7.2 変更ファイル一覧

| # | ファイルパス | 変更内容 |
|---|------------|---------|
| 1 | `/src/todo-service/api/Models/Todo.cs` | TodoTagsナビゲーションプロパティ追加 |
| 2 | `/src/todo-service/api/Models/List.cs` | Tagsナビゲーションプロパティ追加 |
| 3 | `/src/todo-service/api/Data/TodoDbContext.cs` | DbSet追加、OnModelCreating拡張 |
| 4 | `/src/todo-service/api/DTOs/TodoDtos.cs` | Tag関連DTO追加、TodoResponse/ListDetailResponseにtagsフィールド追加 |
| 5 | `/src/todo-service/api/Controllers/TodosController.cs` | ToDoタグ付与・解除アクション追加、タグInclude追加 |
| 6 | `/src/todo-service/api/Controllers/ListsController.cs` | タグInclude追加 |
| 7 | `/src/todo-service/db/seed.sql` | サンプルタグデータ追加 |
| 8 | `/src/bff-service/api/Controllers/TodosController.cs` | ToDoタグルート追加 |
| 9 | `/src/web/src/app/models/index.ts` | TagModel, TagSummary, リクエスト型追加、TodoModelにtags追加 |
| 10 | `/src/web/src/app/components/list-detail/list-detail.component.ts` | タグ管理UI・表示・フィルタリング追加 |

---

## 付録A: 整合性チェック結果

本統合にあたり発見・解消した不整合事項を以下に記録する。

### A.1 UC-03とMVP権限マトリクスの不整合（解消済み）

| 項目 | 内容 |
|------|------|
| **不整合内容** | ビジネス要件UC-03では「editorのタグ作成を制限したい」というシナリオが記述されているが、システム要件の権限マトリクスではeditorにタグ作成・編集・削除の全権限が付与されている |
| **原因** | UC-03のシナリオはUS-PRM-02（Could優先度、Phase 3）の機能を前提としており、MVP時点の権限設計とは異なるフェーズの要件が混在していた |
| **解消方法** | UC-03に整合性注記を追加し、MVPではeditorがタグCRUD可能であること、権限制限はPhase 3対応であることを明記した。権限設計セクション（4章）でもPhase 1とPhase 3の権限マトリクスを分けて記載した |

### A.2 デフォルト色の実装レベル差異（確認済み）

| 項目 | 内容 |
|------|------|
| **確認内容** | ビジネス要件US-LBL-03（Should）は「タグが視覚的に区別できるよう自動割当」を求めているが、システム要件では単一のデフォルト色（#6B7280）のみ定義されている |
| **判断** | MVPでは単一デフォルト色で対応し、US-LBL-03のインテリジェントな色ローテーションはPhase 2で実装する。Phase 1でもユーザーは手動で色を設定可能であり、機能上の問題はない |

### A.3 positionフィールドの先行定義（確認済み）

| 項目 | 内容 |
|------|------|
| **確認内容** | tagsテーブルにpositionカラムが定義されているが、タグ並び順変更UIはPhase 2の機能である |
| **判断** | DB設計段階でpositionカラムを含めることで、Phase 2での追加マイグレーションを不要とする。Phase 1ではDEFAULT 0が適用され、表示順は作成順（created_at）とする |

### A.4 フィルタリングの実装レイヤー（確認済み）

| 項目 | 内容 |
|------|------|
| **確認内容** | フィルタリング機能（US-FLT-01~03）に対応するAPIエンドポイントが定義されていない |
| **判断** | Phase 1のフィルタリングはフロントエンド（Angular）で実装する。既存のGET /api/lists/{listId}/todosレスポンスにtagsフィールドが追加されるため、クライアントサイドでフィルタリングが可能。大量データ対応のサーバーサイドフィルタリングは将来的な拡張として検討する |
