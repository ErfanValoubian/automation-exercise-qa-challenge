# Validation ledger

**The final main run passed 28/28 tests.** This includes real requests to the public API and all four real UI journeys, including the hybrid purchase. No mock application was substituted.

## Tested configuration

- Windows, .NET SDK **8.0.425**, runtime **8.0.31**.
- NUnit **4.2.2**, adapter **4.6.0**, Playwright **1.62.0**, System.Text.Json **8.0.6**.
- Headless Chromium using installed **Microsoft Edge** (`AE_BROWSER_CHANNEL=msedge`).
- Two NUnit workers, independent contexts/data, no automatic retries.
- Target: `https://automationexercise.com`, environment label `public-practice`.
- Main run: **2026-09-18 01:21:31–01:22:18 +03:30**, approximately **47 seconds** including runner overhead.

The sandbox validation restored cached packages plus official NuGet downloads through a local feed. Vulnerability audit was disabled for that local offline restore command only; it remains enabled by default in the delivered project/CI. Sandbox-specific environment normalization is not required by, or embedded in, the delivered test code.

## Results and evidence

| Check | Result | Evidence |
|---|---|---|
| Release build | 0 errors, 0 warnings; warnings treated as errors | Compiled source used by the runs below |
| Main suite | **28 passed / 0 failed / 0 skipped**: 11 API, 4 UI, 13 harness | [HTML](samples/final-all/evidence/index.html), [TRX](samples/final-all/runner/results.trx) |
| Owned-account cleanup | **7 confirmed absent**, 21 tests required no account cleanup; no cleanup warnings | Per-test diagnostics in the main report |
| Empty-search observation | **1 passed**; valid response recorded separately from policy acceptance | [HTML](samples/observations/evidence/index.html), [TRX](samples/observations/runner/results.trx) |
| Browser failure demonstration | **1 intended assertion failure**, real PNG/DOM/trace captured, exit code 1 | [HTML](samples/browser-demo/evidence/index.html), [TRX](samples/browser-demo/runner/results.trx) |
| Offline report demonstration | **1 intended assertion failure**, exit code 1 preserved | [HTML](samples/final-report-demo/evidence/index.html), [TRX](samples/final-report-demo/runner/results.trx) |
| Independent API probes | Three actual response wrappers and ID-set comparison | [Quality reports](QUALITY_REPORTS.md) |

The main suite's four UI journeys all passed: account access/logout, two-product basket/removal, invalid login and the API-arranged purchase. The custom reports retain their individual durations and worker/correlation IDs.

The empty-search observation initially encountered sandbox `EACCES` after a conversation continuation reset network access. It was executed once more after network permission was restored. This was an explicit environment recovery, not a hidden product-test retry. The earlier failed output was retained in the working validation area; the shipped observation sample identifies the successful execution by its own timestamps and correlation ID.

## Remaining account-specific submission steps

The Azure pipeline definition is included, but **no hosted Azure run has been performed**: no target repository or Azure project was supplied. The final local run is execution evidence, not a fabricated CI run. Before assessment submission, publish the repository under your account, identify your name and reviewed commit, and attach the real pipeline URL once it runs.

For reproducibility on your machine:

```powershell
$env:AE_BROWSER_CHANNEL = 'msedge'  # Match the validated browser if Edge is installed
./scripts/run.ps1 -Suite all
./scripts/run.ps1 -Suite observations
./scripts/run.ps1 -Suite demo       # Expected exit 1 from the intentional assertion
```

Alternatively, install the default bundled Chromium and unset `AE_BROWSER_CHANNEL`. Bundled Chromium on Linux CI, Firefox and WebKit have not been executed here; the passing Edge run does not certify that matrix.

## Important limits

- Raw Playwright traces/screenshots may include synthetic form values; custom report redaction is not archive sanitization. The shipped intentional-browser-demo trace uses a local diagnostic page with no customer data.
- Abrupt termination can prevent cleanup; a controlled production test environment would need a leased-data janitor.
- Cleanup warnings remain visible without overwriting the business assertion result.
- The custom HTML index aggregates one test process; TRX is authoritative for discovery/setup failures.
- Two initial UI locator failures were fixed using actual DOM evidence; the supplied final-all report is the subsequent green run with the corrected selectors.
- An initial HttpClient prototype could not use the sandbox's Windows TLS stack. The final supported APIRequestContext transport verifies TLS and passed all API tests. No TLS bypass is present.

The package includes an offline Git bundle containing actual implementation and verification commits. To preserve that history, run `git clone repository.bundle qa-automation` from the extracted package directory. The ZIP source and the bundle's main branch correspond to the same final commit.

## Consent-overlay correction (2026-09-18)

A subsequent user run passed 27/28 main tests: the purchase click was blocked by the site's Funding Choices consent overlay. BrowserSession now registers a Playwright locator handler before navigation. When that overlay appears, it opens Manage options and confirms existing choices through the visible preferences panel, then resumes the original action.

Validation after this correction: Release build succeeded with zero warnings/errors; all 3 new browser-harness checks passed; all 4 public-site UI scenarios passed using Microsoft Edge. The live rerun did not show the consent overlay, so the timing behaviour is covered by separate local browser fixtures, not claimed as a live CMP validation. All 3 accounts from the UI rerun were deleted and confirmed absent. The original 28-test sample above predates this correction. The subsequent full local run, recorded below, verifies all 28 main tests after the correction.

Run the focused helper checks with `./scripts/run.ps1 -Suite browser-harness`. They are separate from the 28-test `all` suite and require an installed browser. The user's separate NuGet restore collision was not reproduced or diagnosed by this change.

## Latest full verification after the consent correction

The user executed the updated source locally on 2026-09-18. Run identifier: `20260918-094606-all-bdbb72`; source revision: `a85a72d` (subsequent publication preparation changes documentation and packaged evidence).

| Check | Verified result |
|---|---|
| Release build | Succeeded; 0 warnings, 0 errors |
| Main suite | 28 passed, 0 failed, 0 skipped; reported duration 44 seconds |
| Account cleanup | 7 deleted and confirmed absent; 21 tests required no cleanup |
| Consent helper regression | Separate execution: 3 passed, 0 failed |
| Hosted CI | Not yet executed |

Evidence: [latest main HTML](samples/verified-all/evidence/index.html), [latest main TRX](samples/verified-all/runner/results.trx), [consent regression HTML](samples/consent-regression/evidence/index.html). The full run was provided by the user and checked against its local diagnostic files. It is not represented as an independently hosted pipeline run. Build used `--no-restore`; this result does not resolve the previously reported NuGet restore collision.
