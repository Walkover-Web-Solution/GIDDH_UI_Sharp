# GiddhTemplate: Giddh PDF service (GIDDH_UI_Sharp)

A .NET 8 ASP.NET Core service that turns Giddh vouchers, invoices and account statements into PDFs. The Giddh backend POSTs a JSON payload; the service renders a RazorLight template and prints it with headless Chrome (PuppeteerSharp). The only caller is the Giddh backend, server to server. There is no UI and no database. It runs on AWS Elastic Beanstalk, with logs going to Loki, metrics to Prometheus and alerts to Slack.

## Commands

All verified on 2026-11-17 with .NET SDK 8.0.423. [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) has timings and the expected warnings.

| Task | Command |
|---|---|
| Restore | `dotnet restore` |
| Build | `dotnet build` (0 errors and 12 warnings is the baseline; `.cshtml` templates are **not** compiled) |
| Tests | `dotnet test GiddhTemplate.Tests` (9 tests). A bare `dotnet test` runs nothing and still passes. |
| Run | `dotnet run`: `http://localhost:5000`. Locally, first point Chrome at your machine (see below). |
| Render a sample | `curl -o out.pdf -X POST localhost:5000/api/v1/pdf -H 'Content-Type: application/json' -d @sample-payloads/TemplateA-Complete-Sample.json` |
| Publish | `dotnet publish -c Release -o ./out` |
| Lint / format | None configured |

## Reading Order Before You Change Code

1. **[docs/VISION.md](docs/VISION.md)**: why this service exists and what it must not become. Check every request against it.
2. **[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)**: startup, the services, the shared Chrome and its concurrency, background services, and where files go.
3. Then read only what the task touches:
   - [docs/FLOWS.md](docs/FLOWS.md): each request path from controller to PDF, plus failure and recovery.
   - [docs/CONTRACTS.md](docs/CONTRACTS.md): routes, payload models, payload tolerance, errors, metrics, logs and alerts.
   - [docs/DESIGN.md](docs/DESIGN.md): the template catalogue, page geometry, fonts, CSS conventions, and recipes for changing or adding a template.
   - [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md): setup, config keys, EB deploy internals and the verification checklist.
   - [docs/DECISIONS.md](docs/DECISIONS.md): why things are the way they are, and the reverts and incidents behind them.
   - [docs/OPEN_QUESTIONS.md](docs/OPEN_QUESTIONS.md): unknowns and known risks. Read it before "fixing" anything odd.

## Project Rules & Boundaries

1. **Never commit the local Chrome path.** `Services/PdfService.cs:GetBrowserAsync` must keep `/usr/bin/google-chrome`. Swap it only in your working copy, because the Mac path breaks every PDF on EB, which has happened several times ([DEVELOPMENT.md](docs/DEVELOPMENT.md)).
2. **The backend calculates and the templates format.** Never compute totals, taxes or amount-in-words here; they arrive in the payload ([VISION.md](docs/VISION.md)).
3. **Read generic payload data through `SafePayload` and `TemplateHelpers`** (`Services/SafeDynamic.cs`, `Services/TemplateHelpers.cs`), so one bad field degrades one value instead of the whole PDF. Add a test in `GiddhTemplate.Tests` when you change them.
4. **Don't change Chrome flags, concurrency limits, restart schedules or memory settings in passing.** History is full of push-and-revert cycles on these, and they caused a production memory crisis ([DECISIONS.md](docs/DECISIONS.md), `MEMORY_CRISIS_RCA_AND_FIXES.md`). Any change here needs its own PR, a reason, and a load check.
5. **Always release Chrome pages and temp files** on every path, error paths included. `PdfCleanupService` is the safety net, not the plan.
6. **Render and look at the PDF for every template change.** `dotnet build` doesn't compile `.cshtml`, and templates, CSS and fonts are cached until restart, so restart before each check ([DESIGN.md](docs/DESIGN.md#visual-check)).
7. **A new template folder or file type must be shipped by the csproj.** `EnableDefaultContentItems` is `false`, so check the `Content Include` items in `GiddhTemplate.csproj` and confirm with `dotnet publish`.
8. **A shared template stays generic.** Never hard-code one customer's data, labels or layout; drive it from the payload or theme.
9. **Never log secrets or customer payloads.** Name config keys (`SLACK_WEBHOOK_URL`) and never print their values. Two current violations are listed in [OPEN_QUESTIONS.md](docs/OPEN_QUESTIONS.md#security--operational-risks-found-while-documenting-not-fixed); don't add more.
10. **Keep `SLACK_WEBHOOK_URL` unset locally,** so test failures don't page the team.
11. **New PDF types follow the existing pattern:** a controller, a service with its own semaphore and timeout, a template folder and a sample payload ([ARCHITECTURE.md](docs/ARCHITECTURE.md#how-to-add-a-new-pdf-type)). Document the route and payload in `CONTRACTS.md`.
12. **No new callers or public endpoints.** Browser-facing routes, CORS or end-user auth are out of scope without an owner decision.
13. **Never invent payload fields** in `sample-payloads/` or templates to make a feature look finished. A field the backend doesn't send yet goes into `CONTRACTS.md` as a required contract.

## Where Things Are

- `Program.cs`: startup, DI, middleware, Serilog, OpenTelemetry, Prometheus and the global exception handler with its Slack alert.
- `Controllers/`: `PdfController` (invoices and vouchers), `GenericPdfController` (named templates), `AccountStatementController`, `DiagnosticsController`.
- `Services/`: the three PDF services, the Razor template services, safe-payload helpers, the Chrome, memory and cleanup background services, and Slack.
- `Models/`: request models and enums. `Converters/`: JSON number handling.
- `Templates/`: `Templates/TemplateA/`, `Templates/Tally/`, `Templates/Thermal/`, `Templates/AccountStatement/`, `Templates/other-template/` (generic) and `Templates/Fonts/`.
- `sample-payloads/`: safe synthetic payloads for local renders.
- `GiddhTemplate.Tests/`: xUnit tests for the safe-payload helpers.
- `.ebextensions/`, `.platform/`, `deps/`: EB instance setup (Chromium RPM, fonts, Grafana Alloy, swap, nginx).

## PR & Deployment Flow

Confirmed by the owner, 2026-11-17. There is no CI.

- **Branch:** from `master`, named by its ClickUp task id (often `<id>-master`).
- **Before the PR:**
  - `dotnet build` and `dotnet test GiddhTemplate.Tests` pass.
  - Every template family you touched renders from a sample payload, and you have looked at the PDF.
  - The Chrome path is the server path.
- **PR into `master`:** this deploys to the **TEST** EB environment. Check there.
- **Merge `master` into `production`:** this deploys to **PROD**. What actually runs the deploy isn't in git ([Q-006](docs/OPEN_QUESTIONS.md)).
- Agents never commit, push, merge or deploy without the owner saying so for that action.

## Evaluating a Request

Follow [docs/VISION.md](docs/VISION.md#how-future-agents-must-evaluate-requests):
- **Make a template request concrete:** name the template, the voucher type and a sample payload.
- **Push back** on calculation, persistence or new callers; say why.
- **Record direction changes** in `VISION.md` in the same change.
- **Describe missing data as a payload contract;** never fake it.

## Verification & Knowledge Maintenance

- **Baseline first:** run the build and tests before editing. 12 warnings are expected.
- **Say what you verified and what you couldn't.** EB-only behaviour (nginx, hooks, environment properties, Linux fonts) can't be checked locally. Say so, and check on TEST.
- **Preserve:** routes, payload tolerance, error status codes, PDF appearance, fonts, the 200-with-PDF behaviour, and the restart and cleanup schedules, unless the change is about them.
- **Update the owning doc in the same change,** and bump its `verified_at`. <!-- verify:ignore -->

  | What changed | Doc to update |
  |---|---|
  | Routes or payloads | `CONTRACTS.md` |
  | Services, lifecycle | `ARCHITECTURE.md` |
  | Request paths | `FLOWS.md` |
  | Templates, fonts, CSS | `DESIGN.md` |
  | Config, EB, commands | `DEVELOPMENT.md` |
  | A reversed choice | `DECISIONS.md` |
  | A new unknown | `OPEN_QUESTIONS.md` |

- **Old docs:** `README.md`, `COMPLETE_DOCUMENTATION.md`, `DEVELOPER_GUIDE.md` and `docs/{controllers,models,pdf-generation,program,services,setup,templates}.md` are superseded and partly wrong. Don't trust or update them. `MEMORY_CRISIS_RCA_AND_FIXES.md` stays as the incident record.
