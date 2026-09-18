# Actual execution samples

Open [the latest verified main report](verified-all/evidence/index.html) first: **28 passed, 0 failed**, after the consent-overlay correction.

| Folder | Meaning |
|---|---|
| `verified-all/` | Latest full local execution after consent handling was corrected; 28 passed |
| `consent-regression/` | Three isolated browser checks for unexpected consent-overlay timing; all passed |
| `final-all/` | Original API + UI/hybrid + harness run, real public application, Edge Chromium, two workers |
| `observations/` | Separate empty-input contract observation; passing does not mean a product-policy decision |
| `browser-demo/` | Deliberately failing local-page browser test; PNG, trace, DOM, JSON and HTML exist |
| `final-report-demo/` | Deliberately failing offline assertion; tests report plumbing without a browser |
| `public-api/` | Independent public API response captures plus derived product-ID comparison |

These files are actual generated outputs. Intentional demos should be red; the main suite should be green. Raw runner files may contain original host paths and names. Relative links in `evidence/index.html` and `details.html` work after extracting the whole directory tree. The duplicate attachments under `runner/` are the original VSTest copies.

Browser demo correlation: `6cf23c635bde4e58a9e47604724f7266`.

- [Screenshot](browser-demo/evidence/6cf23c635bde4e58a9e47604724f7266/failure.png)
- [Playwright trace](browser-demo/evidence/6cf23c635bde4e58a9e47604724f7266/trace.zip)
- [Structured diagnostics](browser-demo/evidence/6cf23c635bde4e58a9e47604724f7266/diagnostics.json)

Trace contains a local diagnostic page, not a mock purchase flow. Public business journeys are represented separately in `final-all/`. See [VALIDATION.md](../VALIDATION.md) for scope and remaining hosted-CI verification.
