# Repository handoff

## Review order

1. Read the root README for setup, architecture and execution commands.
2. Review `docs/TEST_STRATEGY.md` for scope and test selection.
3. Open `docs/samples/verified-all/evidence/index.html` locally for the latest complete run.
4. Review `docs/QUALITY_REPORTS.md` for evidence-backed product observations.
5. Read `docs/LOGISTICS_TEST_DESIGN.md` for the design-only extension.
6. Review `docs/AI_ENGINEERING_NOTE.md` for AI assistance and human review responsibilities.

## Verified baseline

The latest complete local run passed 28 tests with no failures or skipped tests. Three additional browser-helper checks passed separately. Exact provenance, environment and limits are recorded in `docs/VALIDATION.md`. Intentional failure samples are kept separate from successful acceptance evidence.

## Viewing reports

Download or clone the repository and open the HTML report in a browser. Git hosting file viewers generally show HTML source rather than execute the report. Keep each report's sibling directories so its relative links continue to work. TRX files provide machine-readable results; per-test JSON records cleanup and failure classifications.

## Publishing and CI

The repository includes `azure-pipelines.yml` and its job template. Connect the repository to an Azure DevOps pipeline and select that YAML file to execute the required staged checks. Repository hosting and Azure pipeline execution are separate steps. Add a real pipeline-run link only after execution; no hosted result is currently claimed.

Do not commit build outputs, local account-retention files or arbitrary run directories. Curated samples under `docs/samples` are intentional evidence. No software license has been selected; repository visibility alone does not grant a license.
