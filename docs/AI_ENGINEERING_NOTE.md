# AI engineering note

This implementation was authored with OpenAI Codex in a user-requested rewrite. The candidate remains responsible for understanding and defending it. No subagents were used. The earlier submitted implementation was reviewed for defects; this package uses newly authored source, architecture and documents, not a renamed copy.

Prompt patterns: derive a small traceable set of business risks from the assessment; keep public-application automation separate from written logistics design; make reports expose the original failure and cleanup independently; treat unspecified behavior as an observation until a contract exists. Supporting APIs were checked against official Microsoft, Playwright and Automation Exercise documentation.

Generated work: C# support code, NUnit API/UI/hybrid/harness tests, PowerShell scripts, Azure YAML, HTML diagnostics and Markdown documentation. Verification: .NET build, focused framework regression tests, real API/UI executions, explicit report/browser failures, HTML/TRX inspection and independent public API probes. `VALIDATION.md` gives exact final results and environment restrictions; it does not claim hosted CI passed.

Rejected suggestion: make empty-search return-all a build-breaking product defect, or hide it by weakening all assertions. The API only documents absence of the parameter. The rewrite isolates that observation while continuing to reject malformed/non-JSON responses and invalid application codes.

Incorrect generated assumption discovered and corrected: calling `JsonElement.TryGetInt32` without first checking `ValueKind` can throw for a string response code. A focused harness test now verifies that a string code is rejected through a clear contract error. Review also found that fixture-level NUnit categories must be combined with method properties for accurate report classification; the report adapter now does that.

Another corrected assumption: a demo category does not imply an intentional failure; a missing browser must still be reported as setup failure. Focused regression tests cover that distinction. Real UI execution also exposed icon prefixes in accessible link names; header-scoped suffix matching corrected the selectors.

Limits: synthetic traces are not globally sanitized; hosted CI requires the candidate's repository/account; a public UI can change or show consent/advertising overlays. Playwright was updated to the NuGet-published 1.62.0 release; APIRequestContext replaced the native-TLS-dependent HttpClient prototype without disabling TLS checks. Dependencies are pinned, and the exact tested channel is stated in the validation ledger.
