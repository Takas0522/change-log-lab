param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("smoke", "regression")]
    [string]$Suite,

    [string]$AppPath = ".\src\OrderClientApp.Wpf\bin\Release\net10.0-windows\OrderClientApp.Wpf.exe",

    [string]$ArtifactsRoot = ".\artifacts\e2e"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
$suitePath = Join-Path $repoRoot "tests\wpf-native-e2e\winappcli\suites\$Suite.json"
$appFullPath = Join-Path $repoRoot $AppPath
$artifactPath = Join-Path $repoRoot "$ArtifactsRoot\$Suite"

if (-not (Test-Path $suitePath)) {
    throw "Suite file not found: $suitePath"
}

if (-not (Test-Path $artifactPath)) {
    New-Item -ItemType Directory -Path $artifactPath -Force | Out-Null
}

$winAppCli = Get-Command "winappcli" -ErrorAction SilentlyContinue
if ($null -eq $winAppCli) {
    Write-Warning "winappcli not found. Contract-only mode."
    Write-Host "Command contract:"
    Write-Host "winappcli run --suite `"$Suite`" --suite-file `"$suitePath`" --app `"$appFullPath`" --artifacts `"$artifactPath`""
    exit 0
}

if (-not (Test-Path $appFullPath)) {
    throw "App executable not found: $appFullPath"
}

& $winAppCli.Source run --suite "$Suite" --suite-file "$suitePath" --app "$appFullPath" --artifacts "$artifactPath"
exit $LASTEXITCODE
