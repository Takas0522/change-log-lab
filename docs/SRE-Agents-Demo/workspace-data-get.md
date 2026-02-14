---
name: WorkspaceDataGet
description: ワークスペースからデータを取得するためのKQLを構築し結果をJSONのまま返却します。Skillの実行時にワークスペースIdか、ResourceIdと取得したいデータを指定する必要があります。
tools:
  - QueryLogAnalyticsByResourceId
  - QueryLogAnalyticsByWorkspaceId
---

下記の手順でWorkspaceのデータを取得しその結果をそのまま返却します。データの分析・加工はSkillの実行元で実施するため加工をおこなってはいけません。

1. コンテキストから得られた取得したい情報をもとにKQLを構築します
2. 構築されたKQLをもとに下記ツールを使用しデータを取得し返却します
  - QueryLogAnalyticsByResourceId: リソースIDが指定された場合使用
  - QueryLogAnalyticsByWorkspaceId: WorkspaceIdが指定された場合使用