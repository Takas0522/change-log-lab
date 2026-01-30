# 要求定義文書 INDEX

このディレクトリには、ISO/IEC/IEEE 29148に準拠した要求仕様書（SRS: Software Requirements Specification）が格納されています。

## 文書一覧

| 文書ID | 文書名 | 対象機能 | 作成日 | 最終更新日 | ステータス | バージョン |
|---|---|---|---|---|---|---|
| SRS-TODO-001 | 高機能ToDoマネジメントシステム要求仕様書 | ToDo管理・ラベル管理・フィルタリング | 2024-01-15 | 2024-01-31 | ✅ Approved | 1.1 |

## レビュー履歴

| レビューID | 対象文書 | レビュー日時 | レビュー回数 | 判定 | レビュー結果ファイル |
|---|---|---|---|---|---|
| REV-001 | SRS-TODO-001 v1.0 | 2024-01-30 15:10:00 | 1/3 | ❌ 不合格 | REVIEW-RESULT-001.md |
| REV-002 | SRS-TODO-001 v1.1 | 2024-01-31 15:30:00 | 2/3 | ✅ 合格 | REVIEW-RESULT-002.md |

## ステータス定義

| ステータス | 説明 |
|---|---|
| Draft | 作成中 |
| Review | レビュー中 |
| Approved | 承認済み |
| Deprecated | 廃止 |

## 命名規則

```
SRS-{機能ID}-{機能名}.md
```

例: `SRS-001-user-authentication.md`

## ディレクトリ構造

```
docs/要求定義/
├── INDEX.md                                    # この索引ファイル
├── SRS-TODO-001-todo-management-system.md      # 要求仕様書 v1.1 (✅ 合格)
├── REVIEW-RESULT-001.md                        # 第1回レビュー結果 (❌ 不合格)
├── REVIEW-RESULT-002.md                        # 第2回レビュー結果 (✅ 合格)
├── PR-COMMENT-002.md                           # PR用コメントサマリー
└── 修正ガイド.md                                # レビュー指摘事項の修正ガイド
```

## 参照

- ISO/IEC/IEEE 29148:2018 - Systems and software engineering — Life cycle processes — Requirements engineering
- 詳細設計: `docs/詳細設計/INDEX.md`
