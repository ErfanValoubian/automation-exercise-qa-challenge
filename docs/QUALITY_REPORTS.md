# Evidence-based observations and quality risks

These are deliberately classified as contract inconsistencies/risks, not unconfirmed critical product defects. Probe evidence was captured from the public application on **2026-09-17 at 21:31 UTC** (2026-09-18 locally). The independent Node HTTP probes reached the service while the initial HttpClient prototype encountered a Windows sandbox TLS failure. The final C# transport uses Playwright APIRequestContext; its separately recorded results are in VALIDATION.md. Probe results must not be misrepresented as C# test executions.

## QR-001 — Empty search returns the complete catalogue; policy unspecified

| Field | Value |
|---|---|
| Capability | Product search, `POST /api/searchProduct` |
| Classification | Observed behavior / contract ambiguity |
| Severity | Minor for this small practice catalogue; impact on large catalogues is unmeasured |
| Priority | P2: clarify expected behavior before making it a release gate |
| Preconditions | Public service reachable; no account required |
| Environment | Public HTTPS endpoint; independent Node 20.17.0 fetch probe |
| Observation identifier | `empty-search-probe`, UTC 2026-09-17T21:31:31.192Z |
| Reproducibility | One fresh probe in this package; prior user-supplied run independently showed the same count. Not a claim about every deployment. |

Steps:

1. Send form-encoded `search_product=` to `/api/searchProduct`.
2. Fetch `/api/productsList`.
3. Compare the product ID sets, not only counts.

Expected: the published contract should explicitly distinguish absent and empty values and state whether empty means validation error, empty results or browse-all. A response must retain a valid typed envelope. There is currently no published requirement that empty **must** be rejected.

Actual: HTTP 200 / application `responseCode: 200`; the response contains the same 34 IDs as the full catalogue. The API catalogue describes missing parameter rejection but does not resolve the present-empty case.

Evidence: [empty-search response](samples/public-api/empty-search-probe.json), [catalogue response](samples/public-api/catalogue-probe.json), [comparison](samples/public-api/comparison.json). The probe identifiers are evidence filenames; no server-side correlation support is claimed.

Impact: callers cannot reliably predict empty-search UX from the published documentation. Potential performance consequences depend on indexing, pagination and actual traffic; no load test or full-table-scan claim is made.

Recommendation: document the intended semantics, add pagination/limits if browse-all is intended, and then promote the agreed expectation into regression coverage. Today `ContractObservations.EmptySearch_RecordsSemanticsWithoutInventingARequirement` records it separately and still fails on malformed responses.

## QR-002 — Validation error is represented in JSON while HTTP indicates success

| Field | Value |
|---|---|
| Capability | Search validation, `POST /api/searchProduct` without the parameter |
| Classification | Observed transport/application divergence and documentation ambiguity |
| Severity | Moderate integration risk: HTTP-only clients/metrics can miss a validation failure |
| Priority | P2: explicitly document the envelope/status contract |
| Preconditions | Public service reachable; no account required |
| Environment | Same public endpoint and independent Node probe |
| Observation identifier | `missing-search-probe`, UTC 2026-09-17T21:31:30.322Z |
| Reproducibility | One captured request; scope is this endpoint and this request, not all API error paths |

Steps:

1. Send a POST with an empty form body, omitting `search_product` entirely.
2. Read both the HTTP status and the JSON body.

Expected: a caller should be able to determine from the documentation whether the listed response code is an HTTP status or a JSON application field. For an envelope-based API the documentation should explicitly require application-code inspection; for ordinary HTTP error semantics the status would be 400.

Actual: HTTP status **200**, JSON `responseCode` **400**, with a message identifying the missing parameter. The documented "Response Code: 400" is not clearly qualified as transport versus application status.

Evidence: [captured response](samples/public-api/missing-search-probe.json) and the [published API catalogue](https://automationexercise.com/api_list), API 6. No account data is present.

Impact: generic HTTP-success checks alone are insufficient for this service, and HTTP-status-based error metrics can undercount validation errors. This is not evidence that every client is broken.

Recommendation: specify the protocol unambiguously and align monitoring/client examples with it; consider adopting HTTP 400 with a versioned migration if compatibility permits. The current suite separately records both codes and asserts the application's error code plus meaningful message.

## Reproduce independently

```powershell
curl.exe -i -X POST https://automationexercise.com/api/searchProduct --data 'search_product='
curl.exe -i -X POST https://automationexercise.com/api/searchProduct --data ''
curl.exe -i https://automationexercise.com/api/productsList
```

These are small functional requests, not load tests. If transport is unavailable, preserve that failure rather than claiming the product behavior changed. The quality report evidence files are the original captured response wrappers; comparison.json is derived from their IDs.
