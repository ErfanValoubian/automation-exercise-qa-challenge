param([ValidateSet('chromium','firefox','webkit')][string]$Browser = 'chromium')
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    & dotnet restore Challenge.sln --configfile NuGet.Config
    if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }
    & dotnet build Challenge.sln -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    & (Join-Path $repoRoot 'tests/Challenge.Tests/bin/Release/net8.0/playwright.ps1') install --with-deps $Browser
    if ($LASTEXITCODE -ne 0) { throw 'Playwright installation failed.' }
}
finally { Pop-Location }
