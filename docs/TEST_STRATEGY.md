# Risk-based test strategy

## Scope and source of truth

Target: the externally operated public Automation Exercise site and [published API catalogue](https://automationexercise.com/api_list). No load, database access, service replacement, security bypass or logistics implementation. Tests create disposable accounts only when the scenario requires one. The observation suite is read-only.

API state is authoritative for customer creation, update, authentication and deletion. Browser state is authoritative for session visibility, basket contents, displayed checkout address and the visible order confirmation. The public API exposes no order-history contract: a visible confirmation plus an invoice link does not prove backend settlement, inventory reservation or durable order persistence. The suite makes none of those claims.

## Coverage selection

| Risk / requirement | Automated coverage | Layer / selection |
|---|---|---|
| Unusable catalogue | Every returned product has stable typed fields, unique positive ID, positive parsed price | API smoke |
| Irrelevant search results | All returned items match a searchable field; non-empty and unique IDs | API smoke |
| Customer cannot access account | Create unique user, authenticate, verify retrieved identity | API smoke |
| Updates lost or deletion ineffective | Update name/city, retrieve unchanged identity, delete, verify lookup and login are refused | API regression |
| Duplicate registration corrupts identity | Duplicate email rejected; original name preserved | API regression |
| Authentication accepts invalid input | Existing user/wrong password, unknown user, missing email | API regression |
| Invalid request unexpectedly succeeds | Missing search parameter; unsupported methods on products and brands | API regression |
| Empty versus missing input ambiguity | Record application/HTTP codes and compare returned ID sets with catalogue | Separate contract observations |
| Logout only changes presentation | Login, name and logout link assertions, logout, reload, anonymous state | UI smoke |
| Basket loses or misprices items | Select two products by ID; assert name, quantity, unit price, total; remove one; verify survivor | UI regression |
| API data not usable in browser purchase | API account → UI login/cart/address/payment/confirmation → API identity → API cleanup | Hybrid/UI smoke |
| Invalid UI login creates a session | Error visible and logged-in identity absent | UI regression |
| Framework hides invalid evidence | Price parsing, HTML/malformed contract rejection, nested redaction | Offline harness |
| Failure bundle cannot be inspected | Explicit offline assertion demo and browser diagnostics demo | Manual, never gates |

There are 11 API cases, 4 UI journeys, 1 contract observation, 13 harness cases and 2 explicit diagnostics demonstrations. Authentication, account management and catalogue/search provide at least three capability areas without multiplying fixtures artificially.

Partitioning: one valid account, one wrong-password account, one nonexistent identity and one missing-field request represent useful authentication classes. Empty and absent search values are different boundaries. Unsupported methods cover two endpoints, not every method/endpoint combination. Stable examples `top` and `dress` assume the practice catalogue retains matching products; an empty catalogue is an environmental/data precondition to triage, not a reason to seed the application.

Excluded: every published UI case, visual pixel comparison, load, real payment processing, exhaustive browser matrices and destructive tests against pre-existing accounts. No wrong-password account-deletion security probe is run because it is unnecessary to meet the functional scope and can complicate ownership semantics.

## Architecture / assertions

Transport and application semantics are separately captured. The currently supported envelope is an HTTP success status with a numeric JSON `responseCode`; error application codes are asserted with meaningful messages. A transition to real HTTP 4xx is a contract change that must be evaluated and deliberately supported, not silently accepted by broadening the assertion. Every divergence is recorded even in passing tests.

Product contracts require object/array shapes and field types used by clients, but allow additional fields and categories. Unknown fields are not a failure. Missing/incorrectly typed required fields are. Prices use a strict currency-aware parser; stripping every nondigit could turn `Rs. 500` into the wrong value or hide malformed data.

Business expectations remain in tests. Page objects express actions and expose semantic targets; they do not invent success criteria. `Scenario` only wires configuration, ownership, browser and reports to NUnit. Report state is the only shared mutable support state and is locked; report output is per-process, not a cross-process database.

## Selectors and synchronization

Prefer dedicated `data-qa`/stable IDs and role/accessibility names. The navigation icon fonts contribute a prefix to some link names, so header-scoped name regexes match the stable label suffix. For catalogue and cart, the site lacks complete test attributes: scope semantic class selectors to product IDs and validate the selected ID. Avoid positional layout XPath, text substring matching of cart rows and arbitrary `.First()` matches. A locator change is made in `Browser/Shop.cs`, not in every test. Ask a real product team for stable attributes on semantic controls.

Navigation waits for DOMContentLoaded, then actionability/observable state. Assertions use Playwright's retrying `Expect` APIs for session names, row counts, address and confirmation. Removing a row is followed by a zero-count assertion. There is no `Thread.Sleep`, `WaitForTimeoutAsync`, global retry or network-idle dependency on an ad-heavy page. Default API request budget is 30 seconds; UI action 20 seconds; navigation 40 seconds; Playwright assertion 5 seconds. Set a justified assertion-specific budget if real measurements show it is needed.

The practice API's CRUD results are treated as synchronous, so a wrong retrieval is not hidden with eventual retries. The asynchronous booking extension explicitly defines different consistency rules in its own document.

## Data lifecycle and parallel execution

NUnit creates a fixture instance per test case. Two workers bound load; API tests and the four UI journeys are eligible for parallel execution. Each test owns its APIRequestContext/driver, customer registry, journal and browser/context. Unique GUID-based names and email addresses contain a sanitized worker identifier. No shared mutable account, static browser, test ordering or login reuse.

Ownership is recorded before create is sent so a successful create with a lost response is still eligible for cleanup. Cleanup runs in TearDown after NUnit has recorded the body outcome, not in an `await using` whose scope can close before that outcome is available. It deletes only registered synthetic identities and verifies lookup returns the absence envelope. A test that already deleted its user remains safe: cleanup accepts an already-absent response and checks absence again.

Cleanup failure never replaces the original assertion. It appears as a warning in both a passing or failing report, with HTTP evidence. The CI business gate does not fail solely for cleanup warnings; repeated warnings require suspending account-creating runs and remediation. No aggressive deletion retry is used. In a production system this should have a separately owned cleanup-health gate.

Default: delete on success and failure. For deliberate local investigation set `AE_RETAIN_FAILED_ACCOUNTS=true`: failed-test accounts are retained and a private credentials manifest is written outside normal artifacts. Never publish this directory. Delete retained data manually through the documented API within 24 hours, then remove the manifest. CI fixes retention to false. An account is not retained just because browser startup was interrupted by cancellation; process kill can bypass TearDown entirely, a known limitation requiring an external janitor in a managed test environment.

## Diagnosis, flakiness and retry policy

1. Open runner TRX and `evidence/index.html`; distinguish setup, assertion and environment failures.
2. Use correlation, worker, target, request/response and screenshot/trace to identify the failing step.
3. Reproduce the exact test in the same browser/configuration and then with two workers. Compare state, not only pass/fail.
4. Product defect: business expectation is agreed and incorrect behavior is reproducible. Test defect: bad locator, unsafe read or incorrect assumption. Environment: TLS/network, 429/5xx or unavailable browser. Generic timeouts remain unclassified between these until reviewed.
5. Do not automatically retry assertions or mutations. One manual rerun may diagnose a verified transient transport failure after health recovery, preserving the first failure. No whole-suite rerun just to get green.

Quarantine requires an issue, owner, evidence, a review deadline within seven days and a separate execution schedule. No existing test is silently quarantined. Proposed removal from a gate must be reviewed with risk justification; persistent quarantine is a defect in the process. The empty-search observation is separate because policy is unspecified, not because it happened to fail.

Measure flaky rate as tests failing first attempt but passing an unchanged diagnostic rerun divided by first-attempt executions, reported with denominator and environment exclusions. Track median/p95 time from first confirmed flaky failure to repair. Store first-attempt evidence; do not count rerun success as a clean first run.

## Feedback budget and evidence

Targets, not benchmarks: harness under 5 seconds excluding build; API smoke under 30 seconds; UI smoke under 2 minutes; full regression under 4 minutes on a healthy public service with two workers. Validation.md records actual observations. Investigate p95 drift rather than simply increasing all timeouts.

Every completed scenario writes redacted JSON and HTML. UI failures independently attempt screenshot, DOM and trace capture. Custom response text and registered secrets are masked; raw trace/screenshot content cannot be promised redacted. Use only synthetic data and limit CI artifact retention in the host project. Parallel index updates are locked; TRX captures NUnit worker evidence. Abrupt process termination or setup before journal creation can omit custom HTML; runner results and CI logs remain necessary.
