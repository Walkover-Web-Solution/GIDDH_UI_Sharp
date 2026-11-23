---
covers: ["*"]
verified_at: d462f69
verified_on: 2026-11-17
---
# Open Questions & Known Ambiguities

Questions the repository does not answer. An owner decides; when one is resolved, update the doc that owns the answer and move the entry to **Resolved** with the date and source.

## Active Questions Table

| ID | Topic / Question | Affected Code | Conflicting Evidence / Current Status | What Would Resolve It |
|---|---|---|---|---|
| Q-001 | Who can reach the service on Elastic Beanstalk? | `Program.cs`, `.platform/nginx/` | No route has auth, including `POST /api/diagnostics/gc` and `/metrics`. No security group, VPC or load balancer config is committed. Network isolation is assumed, not shown. | Someone states the EB network setup; then decide whether diagnostics need a guard |
| Q-002 | Intended render concurrency: one per process, or one per endpoint? | `Services/PdfService.cs`, `Services/GenericPdfService.cs`, `Services/AccountStatementPdfService.cs` | Code comments say one at a time. The code has three one-slot semaphores sharing one `--single-process` Chrome, so up to 3 renders run at once ([ARCHITECTURE.md](ARCHITECTURE.md)) | Owner decision, then align the code or the comments |
| Q-003 | Is the 09:00 IST daily Chrome restart still needed now that the browser is recycled every 10 invoice renders? Should restarts wait for the other paths? | `Services/ChromeRestartService.cs`, `Services/PdfService.cs` | Neither restart takes the generic or account-statement locks, so a render in flight on those paths can be killed (inferred) | Owner decision; a test under load |
| Q-004 | Which timeouts are meant to apply? | `.platform/nginx/cond.f/`, `Services/PdfService.cs` | A request can wait 90 s for a slot and then render for 2 min, but nginx times out at 90 s, or at the default 60 s if `.platform/nginx/cond.f/` (not `conf.d`) is never loaded | Confirm whether that folder name is intentional; agree one budget end to end |
| Q-005 | Is `ASPNETCORE_ENVIRONMENT` (or `ENVIRONMENT`) set on both EB environments? | `Program.cs`, `Services/PdfService.cs` | If neither is set, every PDF is also copied into `Downloads/` and never cleaned up | Read the EB environment properties ([DEVELOPMENT.md](DEVELOPMENT.md)) |
| Q-006 | What turns a merge into an EB deploy, and does it run `dotnet publish`? | `.ebextensions/001_install_chromium.config` | The ship flow is confirmed (`master` → TEST, `production` → PROD), but the pipeline itself isn't in git | Owner names the pipeline (CodePipeline, EB CLI or manual) |
| Q-007 | Which generic `templateType` keys does the Giddh backend send? Are `gst_template_backup`, `tally_template`, `thermal_template` and `LedgerTemplate` still used? | `Services/GenericPdfService.cs`, `Templates/other-template/` | Every folder is wired into the switch, so none is dead in code | Backend team lists the keys it sends; unused folders can then be removed with evidence |
| Q-008 | Should generic `TALLY` / `THERMAL` exist? | `Services/GenericPdfService.cs` | They point at folders with no `Template.cshtml`, so they always return 500 | Backend confirms nobody sends them; then remove or fix |
| Q-009 | Should a generic render failure alert Slack? | `Services/GenericPdfService.cs` | The caller gets 200 and a "could not be rendered" placeholder PDF; the failure only reaches the console | Owner decision |
| Q-010 | Should "busy" or "timed out" return 503/429 instead of 500? Should the generic route stop returning `ex.Message`? | `Controllers/*.cs` | Everything is a 500 today, and error bodies differ by route ([CONTRACTS.md](CONTRACTS.md)) | Agreement with the backend on retry semantics |
| Q-011 | Why was `--single-process` added back on 2026-10-07? | `Services/PdfService.cs` | `b00d9a2` "temp fixes" gives no reason; history shows repeated push-and-revert of Chrome flags ([DECISIONS.md](DECISIONS.md)) | The author explains, or a load test |
| Q-012 | Are Thermal PDFs printed on thermal rolls, and at what width? | `Templates/Thermal/` | "Thermal" is an A4 page with a 60%-width table | Product answer |
| Q-013 | Should the bundled fonts cover ₹ and Indic scripts? | `Services/PdfService.cs` | `@font-face` unicode-range is Latin only; other glyphs come from the server's system fonts, so local and EB output can differ | Product / design decision |
| Q-014 | Why the −2px font-size offset? | `Templates/` (see `fe421e9`, `61b35a4`) | Unclear whether it is a product rule or compensation for Chrome scaling | The author explains |
| Q-015 | Do we want visual regression tests (golden PDFs)? | `GiddhTemplate.Tests/` | Tests cover only the safe-payload helpers; templates aren't compiled at build time | Owner decision |
| Q-016 | What was "VM", the system that rendered these templates before, and is it off? | — | See the TODO in [DECISIONS.md](DECISIONS.md) | The author explains |
| Q-017 | Priorities and success measures | — | Nothing written down | Owner input into [VISION.md](VISION.md) |

## Security & Operational Risks Found While Documenting (not fixed)

Init mode is docs only, so nothing below was changed. Each needs a ticket.

- **Webhook URL in logs:** `Services/SlackService.cs` prints the full `SLACK_WEBHOOK_URL` to stdout on every alert, and stdout is shipped to Loki and EB logs.
- **Old webhook in git history:** a hard-coded webhook URL (a viaSocket flow) is visible at `d09e6e0` and was removed in `a480da7`. **Rotate it**: removing it from the tree does not remove it from history.
- **Customer data in logs:** `GenericPdfService` logs the first 2,000 characters of every generic payload to stdout.
- **Server GC is still on in published builds:** `dotnet publish -c Release` writes `"System.GC.Server": true` because the web SDK overrides `runtimeconfig.template.json`. That undoes part of the memory-crisis fix. Setting `ServerGarbageCollection=false` in the csproj would apply it ([DEVELOPMENT.md](DEVELOPMENT.md)).
- **Postdeploy hook may never run:** the Grafana Alloy setup hook is committed without the executable bit, and EB runs only executable hooks. Check `/var/log/eb-hooks.log` on an instance.
- **Local logs may reach production Loki:** local runs may ship logs to production Loki labelled "Production", because the Development settings don't remove the Loki sink. This is inferred from config merging and not observed.
- **Duplicate log sinks:** Serilog sinks are configured twice, so events are duplicated.
- **Mac rendering:** locally, Chrome is only looked for at `/usr/bin/google-chrome`, so on a Mac every PDF call returns 500 unless the path is changed locally ([DEVELOPMENT.md](DEVELOPMENT.md)).
- **Dead or mis-wired pieces:**
  - `/api/diagnostics/memory` always returns `reservation: null`.
  - Swagger is referenced but not wired, so `/swagger` returns 404.
  - `GiddhTemplate.http` calls `/weatherforecast`, which doesn't exist.
  - The custom Meter and ActivitySource are subscribed to but nothing emits on them.

## Known Technical Limitations & Verification Gaps

- **Unavailable service:** the TEST and PROD Elastic Beanstalk environments, their properties and the deploy pipeline.
  - **Verification gap:** EB-only behaviour was read from config, not observed: the nginx include, the hooks, the Chromium install and the environment variables.
  - **Safe handling:** render locally from `sample-payloads/` (see [DEVELOPMENT.md](DEVELOPMENT.md)), then check on TEST after merging to `master` and before promoting to `production`.
- **Rendering was verified locally only:** all 4 sample payloads rendered on macOS once the Chrome path was changed in a throwaway copy. Fonts can differ on EB (Q-013).

## Resolved

| ID | Answer | Source |
|---|---|---|
| — | The only caller is the Giddh backend, server to server | Confirmed by the owner, 2026-11-17 |
| — | Ship flow: PR into `master` → TEST EB; merge `master` into `production` → PROD EB | Confirmed by the owner, 2026-11-17 |
