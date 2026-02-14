Schediled tasks

# TaskName

Daily

# Response subagents

ApplicationInsightDataGet

# Task details

``` markdown
Application Insight `api-set-agent-lab-takas` を参照します。
下記の通り動作します。
1. api-set-agent-lab-takas で直近1日以内に発生したエラーがない確認します
    - ApplicationInsightDataGet サブエージェントで実行
2. エラーが発生している場合はGitHubIssueCheckAndSubmitに情報を引き渡し実行します。必要な情報は下記のとおりです。Applicaiton Insightから情報を収集し作成してください。
  - Operation ID
  - Event time
  - 発生した問題についての要約
  - 想定される原因
  - 想定される問題が発生した箇所
  - URL
  - Call Stack
  - error.type

すべての工程で日本語で結果が出力されるべきことに留意する必要があります。
```
