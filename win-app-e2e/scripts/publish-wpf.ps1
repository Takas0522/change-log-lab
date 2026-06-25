param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

dotnet publish ".\src\OrderClientApp.Wpf\OrderClientApp.Wpf.csproj" `
    -c $Configuration `
    -p:PublishProfile=FolderProfile

Write-Host "Publish completed. See artifacts\publish\OrderClientApp\"
