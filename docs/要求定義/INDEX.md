# 要求定義文書 INDEX

このディレクトリには、ISO/IEC/IEEE 29148に準拠した要求仕様書（SRS: Software Requirements Specification）が格納されています。

## 文書一覧

| 文書ID | 文書名 | 対象機能 | 作成日 | ステータス |
|---|---|---|---|---|
| SRS-001 | [タグラベル機能 統合仕様書](SRS-001-tag-label.md) | タグラベル機能 | 2026-02-09 | Draft |

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
├── INDEX.md          # この索引ファイル
├── SRS-001-xxx.md    # 要求仕様書
├── SRS-002-xxx.md
└── ...
```

## 参照

- ISO/IEC/IEEE 29148:2018 - Systems and software engineering — Life cycle processes — Requirements engineering
- 詳細設計: `docs/詳細設計/INDEX.md`
