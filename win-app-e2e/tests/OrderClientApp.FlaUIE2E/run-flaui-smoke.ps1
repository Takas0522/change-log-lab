param(
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$projectRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$appProject = Join-Path $projectRoot "src\OrderClientApp.Wpf\OrderClientApp.Wpf.csproj"
$testProject = Join-Path $PSScriptRoot "OrderClientApp.FlaUIE2E.csproj"

Write-Host "Building WPF app ($Configuration)..."
dotnet build $appProject -c $Configuration
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$env:ORDER_CLIENT_APP_CONFIGURATION = $Configuration

Write-Host "Running FlaUI smoke tests..."
dotnet test $testProject -c $Configuration --logger "trx;LogFileName=flaui-smoke.trx"
exit $LASTEXITCODE

