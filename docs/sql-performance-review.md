# SQLパフォーマンスレビュー結果

## 📊 実行計画分析

### 概要
- **実行時間**: 0.159 ms (データ量が少ないため高速)
- **計画時間**: 1.338 ms
- **対象クエリ**: 過去2年間のTodo統計を月別に集計

### 実行計画の主要ポイント
1. **Sequential Scan on todos** (Cost: 0.00..12.10, Rows: 40 → 33)
   - `created_at >= NOW() - INTERVAL '2 years'` のフィルタ条件
   - インデックスが使用されていない
   
2. **Sequential Scan on lists** (Cost: 0.00..11.30, Rows: 130 → 9)
   - 全件スキャン（データ量が少ないため影響は軽微）

3. **Hash Join** (Cost: 12.60..24.89)
   - `t.list_id = l.id` での結合

4. **Sort** (Cost: 25.95..26.05)
   - `DATE_TRUNC('month', t.updated_at)` での月別ソート
   - メモリ使用: 27kB

## ⚠️ 特定された問題

### 【Major】インデックス不足
**重要度**: Major  
**影響範囲**: データ量増加時にパフォーマンスが大幅に劣化

#### 問題1: `todos.created_at` にインデックスがない
- 現状は Sequential Scan を実行
- データ量が増加すると線形的にスキャンコストが増大
- WHERE句で頻繁に使用される条件のためインデックスが必須

#### 問題2: 複合インデックスの欠如
- `list_id` と `created_at` の組み合わせでフィルタとJOINを実行
- 複合インデックスがあれば、より効率的なIndex Scanが可能

### 【Minor】クエリロジックの不整合
**重要度**: Minor  
**影響**: データの正確性に疑問

- WHERE句で `created_at` を使用してフィルタリング
- GROUP BY / ORDER BY では `updated_at` を使用
- 「作成日から2年以内」のTodoを「更新月」でグループ化する意図が不明確
- ビジネスロジックとして正しいか確認が必要

## 💡 推奨改善案

### インデックス推奨事項

#### 推奨1: created_at へのインデックス追加（優先度: 高）

```sql
-- 追加推奨インデックス
CREATE INDEX idx_todos_created_at ON todos(created_at);
```

**根拠**:
- WHERE句で `created_at >= NOW() - INTERVAL '2 years'` を頻繁に使用
- 範囲検索に対してB-treeインデックスが効果的
- Seq Scan → Index Scan へ変更可能

**期待効果**:
- データ量が1000件以上になった場合、検索速度が10倍以上向上
- バッファヒット率の改善

#### 推奨2: 複合インデックスの追加（優先度: 中）

```sql
-- 追加推奨インデックス（JOINとフィルタの最適化）
CREATE INDEX idx_todos_list_created ON todos(list_id, created_at);
```

**根拠**:
- JOIN条件 (`list_id`) とWHERE条件 (`created_at`) を同時に最適化
- Index Only Scan の可能性（カバリングインデックス）
- 既存の `idx_todos_list` (list_id単独) と併用

**注意事項**:
- 書き込みパフォーマンスへの影響は軽微（Todoの作成/更新頻度による）
- `idx_todos_list` との重複を考慮し、使用状況に応じて選択

### クエリ最適化

#### オプション1: ロジック修正（created_at で統一）

```sql
-- 元のクエリ
SELECT
    DATE_TRUNC('month', t.updated_at) as month,  -- ← updated_at を使用
    COUNT(*) as total_todos,
    COUNT(CASE WHEN t.is_completed THEN 1 END) as completed_todos,
    AVG(EXTRACT(EPOCH FROM (t.updated_at - t.created_at))/86400) as avg_days_to_complete
FROM todos t
JOIN lists l ON t.list_id = l.id
WHERE t.created_at >= NOW() - INTERVAL '2 years'  -- ← created_at でフィルタ
GROUP BY DATE_TRUNC('month', t.updated_at)
ORDER BY month DESC;

-- 最適化されたクエリ（作成月で集計する場合）
SELECT
    DATE_TRUNC('month', t.created_at) as month,  -- ← created_at に統一
    COUNT(*) as total_todos,
    COUNT(CASE WHEN t.is_completed THEN 1 END) as completed_todos,
    AVG(EXTRACT(EPOCH FROM (t.updated_at - t.created_at))/86400) as avg_days_to_complete
FROM todos t
JOIN lists l ON t.list_id = l.id
WHERE t.created_at >= NOW() - INTERVAL '2 years'
GROUP BY DATE_TRUNC('month', t.created_at)  -- ← created_at に統一
ORDER BY month DESC;
```

**変更点**:
- `updated_at` → `created_at` に統一
- インデックス `idx_todos_created_at` がGROUP BYにも活用可能

**メリット**:
- ロジックの一貫性向上
- インデックスの効果最大化（WHERE、GROUP BY、ORDER BYで同じカラム使用）

#### オプション2: 部分インデックスの活用（上級）

```sql
-- 部分インデックス（2年以内のデータのみ）
CREATE INDEX idx_todos_recent_created 
ON todos(created_at, list_id, is_completed, updated_at)
WHERE created_at >= NOW() - INTERVAL '2 years';
```

**根拠**:
- クエリの条件に完全マッチ
- インデックスサイズの削減
- より高速なIndex Only Scanが可能

**注意事項**:
- PostgreSQL固有の機能
- メンテナンスが必要（定期的な再作成またはROWの更新）

#### オプション3: 不要なJOINの排除検討

```sql
-- 現在のクエリはlistsテーブルをJOINしているが、lists側のカラムを使用していない
-- もしlist_idの存在チェックのみが目的なら、以下の方が効率的

SELECT
    DATE_TRUNC('month', t.created_at) as month,
    COUNT(*) as total_todos,
    COUNT(CASE WHEN t.is_completed THEN 1 END) as completed_todos,
    AVG(EXTRACT(EPOCH FROM (t.updated_at - t.created_at))/86400) as avg_days_to_complete
FROM todos t
WHERE t.created_at >= NOW() - INTERVAL '2 years'
  AND EXISTS (SELECT 1 FROM lists l WHERE l.id = t.list_id)  -- ← EXISTS に変更
GROUP BY DATE_TRUNC('month', t.created_at)
ORDER BY month DESC;
```

**ただし**:
- `todos.list_id` に外部キー制約がある場合、JOINは不要
- スキーマ確認結果: `list_id UUID NOT NULL REFERENCES lists(id) ON DELETE CASCADE`
- **結論**: 外部キー制約があるため、JOIN自体が不要の可能性が高い

#### 推奨: 最もシンプルな最適化クエリ

```sql
-- + 追加行
-- - 削除行
-- ~ 変更行

SELECT
~   DATE_TRUNC('month', t.created_at) as month,  -- updated_at → created_at
    COUNT(*) as total_todos,
    COUNT(CASE WHEN t.is_completed THEN 1 END) as completed_todos,
    AVG(EXTRACT(EPOCH FROM (t.updated_at - t.created_at))/86400) as avg_days_to_complete
FROM todos t
- JOIN lists l ON t.list_id = l.id  -- 不要なJOINを削除
WHERE t.created_at >= NOW() - INTERVAL '2 years'
~ GROUP BY DATE_TRUNC('month', t.created_at)  -- updated_at → created_at
ORDER BY month DESC;
```

**効果**:
1. ロジックの一貫性向上（作成月で集計）
2. 不要なJOINの削除によるパフォーマンス改善
3. インデックスの効果最大化

## 🔍 その他の考慮事項

### 1. データ量とインデックス戦略
- **現状**: 33行のtodos、9行のlists
- **予測**: ユーザー数・リスト数の増加に伴いtodosが数千〜数万行に成長
- **推奨**: 早期のインデックス導入でスケーラビリティを確保

### 2. 統計情報の定期更新
```sql
-- 定期的なANALYZE実行を推奨
ANALYZE todos;
ANALYZE lists;
```

### 3. EXPLAIN ANALYZEの定期実行
- 本番環境のデータ量でテスト
- インデックス追加前後の比較
- 実行計画の変化を監視

### 4. モニタリング項目
- バッファヒット率（現在: shared hit=5）
- Sequential Scan の頻度
- クエリ実行時間の推移

### 5. ビジネスロジックの確認
**要確認事項**:
- 「過去2年以内に**作成**されたTodo」を「**更新**月」でグループ化する意図
- 以下のケースで結果が変わる可能性:
  - 2年以上前に作成され、最近更新されたTodo → 除外される
  - 2年以内に作成され、更新されていないTodo → created_at月に集計

**推奨**: ビジネス要件に応じて以下を選択
- **作成月で集計**: `WHERE/GROUP BY created_at`
- **更新月で集計**: `WHERE/GROUP BY updated_at`

## 📈 期待される効果

### 短期効果（インデックス追加のみ）
- **現在**: 0.159ms (データ量: 33行)
- **予測**: データ1万行時、インデックスなし → 50-100ms、インデックスあり → 5-10ms
- **改善率**: 約10倍の高速化

### 中期効果（クエリ最適化含む）
- JOIN削除による追加10-20%の高速化
- バッファ使用量の削減
- データベース負荷の軽減

### 長期効果
- スケーラビリティの確保（10万行以上のデータでも安定動作）
- メンテナンス性の向上（ロジックの明確化）
- 他の類似クエリへの適用（created_atフィルタの共通パターン）

## 📝 実装優先度

### Phase 1: 緊急（即時実装推奨）
```sql
-- 1. created_at インデックスの追加
CREATE INDEX idx_todos_created_at ON todos(created_at);

-- 2. ANALYZEの実行
ANALYZE todos;
```

### Phase 2: 重要（ビジネスロジック確認後）
```sql
-- 3. クエリの修正（created_at統一 + JOIN削除）
-- 上記「推奨: 最もシンプルな最適化クエリ」を参照
```

### Phase 3: 検討（データ量増加時）
```sql
-- 4. 複合インデックスまたは部分インデックスの追加
-- データ量とクエリパターンに応じて選択
```

## 🎯 まとめ

### 最重要アクション
1. ✅ `idx_todos_created_at` インデックスの追加
2. ✅ ビジネスロジックの確認（created_at vs updated_at）
3. ✅ 不要なJOINの削除

### リスク評価
- **インデックス追加**: リスク低、書き込み性能への影響は軽微
- **クエリ変更**: ビジネスロジック確認が必須、リグレッションテスト推奨

### 次のステップ
1. 本レビュー内容をチームで確認
2. ビジネス要件の明確化
3. ステージング環境でのテスト実行
4. 本番環境への段階的なロールアウト
