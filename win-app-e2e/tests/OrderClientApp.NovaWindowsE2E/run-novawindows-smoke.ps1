param(
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Release",

    [int] $Port = 4723,

    [switch] $AttachPrelaunchedApp,

    [switch] $InstallNovaWindowsDriver
)

$ErrorActionPreference = "Stop"

$projectRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$appProject = Join-Path $projectRoot "src\OrderClientApp.Wpf\OrderClientApp.Wpf.csproj"
$appExe = Join-Path $projectRoot "src\OrderClientApp.Wpf\bin\$Configuration\net10.0-windows\OrderClientApp.Wpf.exe"
$testProject = Join-Path $PSScriptRoot "OrderClientApp.NovaWindowsE2E.csproj"
$artifactRoot = Join-Path $projectRoot "artifacts\e2e\novawindows"
$logRoot = Join-Path $artifactRoot "logs"
New-Item -ItemType Directory -Force -Path $logRoot | Out-Null

Write-Host "Building WPF app ($Configuration)..."
dotnet build $appProject -c $Configuration

$appiumCommand = Get-Command appium -ErrorAction SilentlyContinue
if ($null -eq $appiumCommand) {
    throw "Appium CLI was not found. Install it with: npm install -g appium"
}

if ($InstallNovaWindowsDriver) {
    Write-Host "Installing/updating appium-novawindows-driver..."
    appium driver install --source=npm appium-novawindows-driver
}

$installedDrivers = appium driver list --installed
if ($installedDrivers -notmatch "novawindows") {
    throw "appium-novawindows-driver is not installed. Re-run with -InstallNovaWindowsDriver or run: appium driver install --source=npm appium-novawindows-driver"
}

$stdoutPath = Join-Path $logRoot "appium-stdout.log"
$stderrPath = Join-Path $logRoot "appium-stderr.log"
$appiumProcess = $null
$launchedAppProcess = $null
$testExitCode = 0

try {
    Write-Host "Starting Appium on port $Port..."
    $appiumProcess = Start-Process `
        -FilePath $env:ComSpec `
        -ArgumentList @("/c", "appium", "--port", "$Port") `
        -PassThru `
        -RedirectStandardOutput $stdoutPath `
        -RedirectStandardError $stderrPath `
        -WindowStyle Hidden

    $statusUri = "http://127.0.0.1:$Port/status"
    $deadline = (Get-Date).AddSeconds(30)
    do {
        try {
            Invoke-RestMethod -Uri $statusUri -TimeoutSec 2 | Out-Null
            break
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    } while ((Get-Date) -lt $deadline)

    Invoke-RestMethod -Uri $statusUri -TimeoutSec 2 | Out-Null

    $env:NOVA_APPIUM_SERVER_URL = "http://127.0.0.1:$Port/"
    $env:ORDER_CLIENT_APP_CONFIGURATION = $Configuration
    $env:ORDER_CLIENT_APP_TOP_LEVEL_WINDOW = $null

    if ($AttachPrelaunchedApp) {
        $testFilters = @(
            "FullyQualifiedName=OrderClientApp.NovaWindowsE2E.NovaWindowsSmokeTests.AdminLogin_ReachesDashboard",
            "FullyQualifiedName=OrderClientApp.NovaWindowsE2E.NovaWindowsSmokeTests.AdminCanCreateOrder_FromOrderManagement"
        )

        foreach ($testFilter in $testFilters) {
            $launchedAppProcess = $null
            $env:ORDER_CLIENT_APP_TOP_LEVEL_WINDOW = $null

            try {
                Write-Host "Starting WPF app before creating the NovaWindows session..."
                $launchedAppProcess = Start-Process -FilePath $appExe -PassThru
                $deadline = (Get-Date).AddSeconds(30)
                do {
                    Start-Sleep -Milliseconds 500
                    $launchedAppProcess.Refresh()
                    if ($launchedAppProcess.MainWindowHandle -ne 0) {
                        break
                    }
                } while ((Get-Date) -lt $deadline)

                if ($launchedAppProcess.MainWindowHandle -eq 0) {
                    throw "The WPF app did not expose a top-level window handle within 30 seconds."
                }

                $env:ORDER_CLIENT_APP_TOP_LEVEL_WINDOW = "0x{0:x}" -f $launchedAppProcess.MainWindowHandle
                $env:ORDER_CLIENT_APP_PROCESS_ID = "$($launchedAppProcess.Id)"
                Write-Host "Attaching to WPF top-level window $env:ORDER_CLIENT_APP_TOP_LEVEL_WINDOW (PID $($launchedAppProcess.Id))."

                Write-Host "Running NovaWindows smoke test: $testFilter"
                dotnet test $testProject -c $Configuration --filter $testFilter --logger "trx;LogFileName=novawindows-smoke.trx"
                if ($LASTEXITCODE -ne 0) {
                    $testExitCode = $LASTEXITCODE
                    break
                }
            }
            finally {
                if ($null -ne $launchedAppProcess -and -not $launchedAppProcess.HasExited) {
                    Write-Host "Stopping WPF app process $($launchedAppProcess.Id)..."
                    Stop-Process -Id $launchedAppProcess.Id
                }
            }
        }
    }
    else {
        Write-Host "Running NovaWindows smoke tests..."
        dotnet test $testProject -c $Configuration --logger "trx;LogFileName=novawindows-smoke.trx"
        $testExitCode = $LASTEXITCODE
    }
}
finally {
    if ($null -ne $appiumProcess -and -not $appiumProcess.HasExited) {
        Write-Host "Stopping Appium process $($appiumProcess.Id)..."
        Stop-Process -Id $appiumProcess.Id
    }

    Get-CimInstance Win32_Process |
        Where-Object { $_.CommandLine -match "node.*appium.*--port $Port" } |
        ForEach-Object {
            Write-Host "Stopping Appium node process $($_.ProcessId)..."
            Stop-Process -Id $_.ProcessId
        }

    if ($null -ne $launchedAppProcess -and -not $launchedAppProcess.HasExited) {
        Write-Host "Stopping WPF app process $($launchedAppProcess.Id)..."
        Stop-Process -Id $launchedAppProcess.Id
    }
}

if ($testExitCode -ne 0) {
    exit $testExitCode
}
