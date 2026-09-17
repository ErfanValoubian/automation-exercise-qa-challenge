param(
    [ValidateSet('all','api','ui','hybrid','api-smoke','ui-smoke','regression','harness','observations','demo','report-demo')]
    [string]$Suite = 'all',
    [ValidateSet('Debug','Release')][string]$Configuration = 'Release',
    [switch]$NoBuild,
    [string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$filters = @{
    all = 'TestCategory=api|TestCategory=ui|TestCategory=harness'
    api = 'TestCategory=api'
    ui = 'TestCategory=ui'
    hybrid = 'TestCategory=hybrid'
    'api-smoke' = 'TestCategory=api&TestCategory=smoke'
    'ui-smoke' = 'TestCategory=ui&TestCategory=smoke'
    regression = 'TestCategory=regression'
    harness = 'TestCategory=harness'
    observations = 'TestCategory=observation'
    demo = 'FullyQualifiedName=Challenge.Tests.Ui.EvidenceDemo.IntentionalFailure_CapturesBrowserEvidence'
    'report-demo' = 'FullyQualifiedName=Challenge.Tests.Harness.ReportingDemo.IntentionalFailure_ProducesPortableDiagnostics'
}
if (-not $OutputDirectory) {
    $runName = '{0}-{1}-{2}' -f (Get-Date -Format 'yyyyMMdd-HHmmss'), $Suite, ([Guid]::NewGuid().ToString('N').Substring(0,6))
    $OutputDirectory = Join-Path $repoRoot "artifacts/$runName"
}
$runDirectory = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $runDirectory -Force | Out-Null
$previousArtifacts = $env:AE_ARTIFACTS
$testExitCode = 1
Push-Location $repoRoot
try {
    if (-not $NoBuild) {
        & dotnet restore Challenge.sln --configfile NuGet.Config
        if ($LASTEXITCODE -ne 0) { throw 'NuGet restore failed.' }
        & dotnet build Challenge.sln --configuration $Configuration --no-restore
        if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    }
    $env:AE_ARTIFACTS = Join-Path $runDirectory 'evidence'
    & dotnet test tests/Challenge.Tests/Challenge.Tests.csproj --configuration $Configuration --no-build --no-restore `
        --filter $filters[$Suite] `
        --logger 'trx;LogFileName=results.trx' `
        --logger 'html;LogFileName=runner.html' `
        --results-directory (Join-Path $runDirectory 'runner')
    $testExitCode = $LASTEXITCODE
    Write-Host "HTML: $(Join-Path $runDirectory 'evidence/index.html')"
    Write-Host "TRX:  $(Join-Path $runDirectory 'runner/results.trx')"
    if ($Suite -in @('demo','report-demo')) { Write-Host 'This demonstration is expected to fail. Its exit code is preserved.' }
}
finally {
    $env:AE_ARTIFACTS = $previousArtifacts
    Pop-Location
}
exit $testExitCode
