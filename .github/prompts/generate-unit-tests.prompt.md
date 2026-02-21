---
description: 指定されたC#クラスに対してISTQB準拠のユニットテストを自動生成するプロンプト
mode: agent
tools:
  - read
  - edit
  - search
  - execute
  - todo
  - usages
---

# ユニットテスト生成

指定されたクラスのユニットテストを ISTQB 準拠で作成してください。

## テスト対象

テスト対象: ${{target_class_or_method}}

## テスト設計手法

以下のテスト設計手法を適用してテストケースを設計してください：

1. **同値分割法**: 入力値を有効/無効のグループに分類し、各グループの代表値でテスト
2. **境界値分析**: パーティションの境界値（最小、最大、境界±1）をテスト
3. **エッジケース**: null、空文字列、空リスト、最大件数、重複データ

## テストフレームワーク

```csharp
// 使用するライブラリ
using Xunit;                     // テストフレームワーク
using Moq;                       // モッキング
using FluentAssertions;          // アサーション
using Microsoft.EntityFrameworkCore; // InMemory DB
```

## 命名規則

```
[メソッド名]_[シナリオ]_[期待結果]
```

例:
- `CreateTodo_WithValidInput_ReturnsTodoResponse`
- `CreateTodo_WithEmptyTitle_ThrowsValidationException`
- `GetTodoById_WithNonExistentId_ReturnsNull`

## テスト構造（AAA パターン）

```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedResult()
{
    // Arrange: テストデータと依存関係のセットアップ

    // Act: テスト対象メソッドの実行

    // Assert: 結果の検証（FluentAssertions を使用）
}
```

## テストカテゴリ

必ず以下のカテゴリのテストを含めてください：

- [ ] **正常系**: 有効な入力での期待動作
- [ ] **異常系**: 無効な入力でのバリデーション・例外
- [ ] **境界値**: 最小値/最大値/空/null
- [ ] **認可**: 権限のないユーザーからのアクセス
- [ ] **データ不在**: 存在しないIDでの検索

## 実行確認

テスト作成後、必ずビルドとテスト実行を確認：

```bash
dotnet test --verbosity normal
```
