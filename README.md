# Automation Exercise QA Challenge

**Author:** [Erfan Valoubian](https://github.com/ErfanValoubian)

A fresh C#/.NET 8 test suite for the public [Automation Exercise](https://automationexercise.com) application. Only test automation is implemented. The logistics extension is a written design.

**Latest verified local execution: 28 passed, 0 failed, 0 skipped.** The suite covers API contracts, customer lifecycle, browser journeys and an API-assisted checkout. See the [validation ledger](docs/VALIDATION.md) and [execution report](docs/samples/verified-all/evidence/index.html). Hosted CI validation is pending.

## Run on Windows

Requirements: .NET SDK 8.0.4xx (8.0.425 was used here), Windows PowerShell 5.1 or PowerShell 7, and HTTPS access to NuGet, Playwright's browser CDN and the public application. Other .NET 8 feature bands require an intentional `global.json` change.

Open PowerShell in the repository root (the folder containing `Challenge.sln`):

```powershell
dotnet restore Challenge.sln --configfile NuGet.Config
dotnet build Challenge.sln -c Release --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/install-browsers.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/run.ps1 -Suite all -NoBuild
```

On Linux/macOS use `pwsh -File` instead of `powershell -NoProfile -ExecutionPolicy Bypass -File`. CI uses PowerShell 7. `--with-deps` installs browser OS dependencies on supported Linux agents. Browser installation happens on **each agent that runs UI**, not only the build job.

## Select suites and generate reports

The runner script builds unless `-NoBuild` is supplied. It always enables TRX and the standard VSTest HTML logger and preserves the real exit code.

```powershell
./scripts/run.ps1 -Suite api
./scripts/run.ps1 -Suite ui
./scripts/run.ps1 -Suite hybrid
./scripts/run.ps1 -Suite api-smoke
./scripts/run.ps1 -Suite ui-smoke
./scripts/run.ps1 -Suite regression
./scripts/run.ps1 -Suite harness
./scripts/run.ps1 -Suite browser-harness
./scripts/run.ps1 -Suite observations
```

`all` contains 28 tests: 11 API, 4 UI and 13 harness checks. The three `browser-harness` checks cover consent-overlay handling separately. Contract observations and intentional failures are separate. The hybrid purchase is also a UI smoke test. Smoke and regression tags are mutually exclusive here.

To use the underlying runner directly:

```powershell
dotnet test Challenge.sln -c Release --filter 'TestCategory=api' --logger 'trx;LogFileName=results.trx' --logger 'html;LogFileName=runner.html' --results-directory artifacts/direct/runner
```

For direct calls without `AE_ARTIFACTS`, custom HTML goes under `tests/Challenge.Tests/bin/Release/net8.0/artifacts/<unique-run>/`; its path is printed in test output. Prefer the script to collect everything together.

Each scripted execution produces:

```text
artifacts/<timestamp>-<suite>-<unique>/
  runner/results.trx             runner result, machine-readable
  runner/runner.html             standard runner summary
  evidence/index.html            human-readable report for all completed scenarios
  evidence/<correlation>/
    details.html                 status, triage, cleanup, timeline, artifact links
    diagnostics.json             structured, redacted evidence (pass AND fail)
    failure.png                  UI failure, if browser is available
    failure-dom.html             UI failure DOM, registered secrets redacted
    trace.zip                    raw Playwright trace for UI failures
```

The custom HTML is self-contained, works offline, uses relative artifact links and escapes untrusted response text. Keep its sibling directories when sharing. TRX remains authoritative for discovery failures and configuration failures before scenario setup completes. The custom index is process-local: give separate jobs/processes different output directories. Do not run multiple processes into the same `AE_ARTIFACTS`.

## Demonstrate diagnostics

```powershell
# Browser evidence, using a local HTML fixture; no account or remote request required
./scripts/run.ps1 -Suite demo
# Offline fallback for the reporting path; no browser required
./scripts/run.ps1 -Suite report-demo
```

Both deliberately fail and return exit code 1. Neither runs in normal gates. They are `[Explicit]` and selected by their exact test names, so broad category matching does not accidentally enable them. A local `SetContentAsync` fixture in the browser demo exists solely to test diagnostics; it is not a mock of the public application.

To replay an actual trace:

```powershell
powershell -File tests/Challenge.Tests/bin/Release/net8.0/playwright.ps1 show-trace '<path-to-trace.zip>'
```

## Configuration

Checked-in `tests/Challenge.Tests/settings.json` contains the public target defaults; `AE_*` environment variables override them. No real account credentials are required or accepted by this test suite. Customers and payment inputs are disposable synthetic values.

| Variable | Purpose / default |
|---|---|
| `AE_BASE_URL` | Public target origin, default in settings.json |
| `AE_ENVIRONMENT` | Report label, `public-practice` |
| `AE_BROWSER` | `chromium`, `firefox` or `webkit`; default chromium |
| `AE_BROWSER_CHANNEL` | Optional installed Chromium channel: `msedge` or `chrome`; unset uses bundled Chromium |
| `AE_HEADLESS` | `true`; set `false` to watch the browser |
| `AE_TIMEOUT_MS` | UI actions: 20000 ms; navigation: twice this value |
| `AE_API_TIMEOUT_MS` | API request budget: 30000 ms |
| `AE_ARTIFACTS` | Exact custom report folder; script sets/restores it |
| `AE_RETAIN_FAILED_ACCOUNTS` | Default false; only retain after a failed test body |
| `AE_PRIVATE_DATA_DIR` | Private retained-credential folder; defaults to test output `.local-retained` |
| `PLAYWRIGHT_BROWSERS_PATH` | Optional standard Playwright browser cache override |

Example:

```powershell
$env:AE_HEADLESS = 'false'
./scripts/run.ps1 -Suite hybrid
Remove-Item Env:AE_HEADLESS
```

Install the selected browser before changing `AE_BROWSER`. No retries are configured. Playwright assertion polling has its own default 5-second budget; it is not governed by `AE_TIMEOUT_MS`. A slow deployment should change this explicitly where justified, not add sleeps.

## Architecture and choices

```text
src/Challenge.Support/
  Api/           HTTP transport, response envelopes, minimal stable product contracts
  Browser/       isolated browser lifetime and capability-oriented page objects
  Data/          unique customer generation, ownership, cleanup and retention
  Diagnostics/   redaction, per-test journal and thread-safe HTML index
  Settings.cs    immutable per-scenario configuration
tests/Challenge.Tests/
  Api/           product/search, authentication, lifecycle and separate observations
  Ui/            account access, cart, purchase, negative login and explicit evidence demo
  Harness/       regression tests for parser/redaction/contract logic; explicit report demo
  Fixtures/      thin lifecycle adapter between NUnit and support objects
scripts/         repeatable local execution
pipelines/       Azure job template
docs/            strategy, logistics, risks, AI note, validation and real samples
```

Playwright was chosen for actionability checks, retrying assertions, fresh contexts and replayable traces. A separate browser per test is intentionally simple and costs more than sharing a browser. With only two workers and four UI scenarios that cost is acceptable; a per-worker browser could be introduced if measurements justify it. Playwright's APIRequestContext keeps API setup independent of browser availability, with a dedicated request context/driver per test and TLS verification enabled. NUnit provides fixture-per-test isolation and explicit diagnostics demonstrations. No DI container or generic automation platform is necessary.

The API transport was changed from an initial HttpClient prototype after this host's native Windows TLS path failed. APIRequestContext is one of the assessment's permitted transports; it uses Playwright's HTTP stack without launching a browser. This change does not disable certificate validation, alter the public service or add retries. The source pins Playwright **1.62.0** and System.Text.Json **8.0.6**.

If Microsoft Edge is already installed, you can run without downloading bundled Chromium:

```powershell
$env:AE_BROWSER_CHANNEL = 'msedge'
./scripts/run.ps1 -Suite all
```

Unset that variable for the default bundled browser. The supplied final browser results identify their channel explicitly; they do not certify every browser engine.

## CI and submission

Create an Azure pipeline using `azure-pipelines.yml`. Jobs run harness → API smoke → UI smoke → regression. Each job restores/builds on its own agent; UI jobs also install the browser. Nonzero test exit codes fail the job. Reports publish on success and failure. Hosted branch-policy validation may need configuring in Azure Repos; the YAML `pr` trigger alone does not create that policy.

The sample local runs do not claim that a hosted pipeline passed. A repository URL and successful CI run are account-specific final submission steps. Commit this source and real sample evidence to your repository, identify your reviewed commit, and add the pipeline run URL after executing it. Submitted by Erfan Valoubian. AI-assisted implementation details are documented in the engineering note.

## Troubleshooting

| Symptom | Action |
|---|---|
| Browser executable missing | Run `scripts/install-browsers.ps1` for the selected browser in the same user/agent context. |
| TLS / `SEC_E_NO_CREDENTIALS` | Inspect the environment event in diagnostics. Repair the host's TLS/proxy/sandbox setup; never disable certificate checks. |
| HTTP 429 or 5xx | Environment/dependency failure; retain evidence and stop rerunning aggressively. |
| HTML instead of JSON | Contract check fails visibly. Inspect whether it is a challenge page or outage; do not bypass security controls. |
| UI timeout | Open the screenshot/trace. Check locator, navigation, consent/ad overlays and service health before changing timeouts. No ad-blocking routes are installed. |
| Cleanup warning on a passed test | The business verdict remains passed; HTML prominently records unconfirmed cleanup. Investigate and clean only accounts owned by that run. |
| No HTML index after setup failure | Use the standard runner HTML/TRX. Invalid configuration can prevent custom scenario setup. |
| Demo returns exit code 1 | Expected only if its recorded cause is the intentional assertion; a missing browser is not a successful demo. |

Raw browser traces and screenshots can contain synthetic form values. Custom redaction does **not** sanitize Playwright's archive. Do not use real credentials or commit raw traces from a real customer environment. Retained credentials are local-only, git-ignored and excluded from normal report publishing; never set `AE_PRIVATE_DATA_DIR` inside an artifacts directory.

## Documents

- [Test strategy and coverage](docs/TEST_STRATEGY.md)
- [Logistics reliability design](docs/LOGISTICS_TEST_DESIGN.md)
- [Evidence-based quality risks](docs/QUALITY_REPORTS.md)
- [AI engineering note](docs/AI_ENGINEERING_NOTE.md)
- [Execution validation and limitations](docs/VALIDATION.md)
- [Repository handoff](docs/REPOSITORY_HANDOFF.md)
