---
covers: ["*"]
verified_at: d462f69
verified_on: 2026-11-17
---
# Architectural & Design Decisions (ADRs)

Recovered from git history (813 commits, 2025-01-24 → 2026-11-13) and `MEMORY_CRISIS_RCA_AND_FIXES.md`. Most commit messages are one line, so many rationales are **not recorded**. Where that is the case, the ADR says so instead of guessing. Shas are short hashes on `master`. Read this before changing Chrome flags, concurrency, memory settings, the Chrome install, or the payload-tolerance layer. Each of these has been flipped and reverted at least once.

## Decision Index

| Date | Decision Record | Status | Area Affected | Key Rationale |
|---|---|---|---|---|
| 2025-02-25 | [ADR-001: PuppeteerSharp + headless Chrome instead of IronPdf](#adr-001-puppeteersharp--headless-chrome-instead-of-ironpdf) | Accepted | Rendering | Not recorded |
| 2025-01-24 | [ADR-002: RazorLight runtime templates, not compiled MVC views](#adr-002-razorlight-runtime-templates-not-compiled-mvc-views) | Accepted | Templates / build | Templates ship as files and are rendered per request |
| 2025-03-07 | [ADR-003: Local font files only, no web fonts or CDNs](#adr-003-local-font-files-only-no-web-fonts-or-cdns) | Accepted | Templates / fonts | Headless Chrome font failures; CDN timeouts |
| 2025-06-18 | [ADR-004: Alert failures to Slack from the service itself](#adr-004-alert-failures-to-slack-from-the-service-itself) | Accepted | Observability | Not recorded |
| 2025-09-04 | [ADR-005: Grafana stack (Loki, Alloy, OTLP traces, Prometheus)](#adr-005-grafana-stack-loki-alloy-otlp-traces-prometheus) | Accepted | Observability | Centralised logging (commit titles) |
| 2026-01-22 | [ADR-006: Stream PDFs through temp files, not byte arrays](#adr-006-stream-pdfs-through-temp-files-not-byte-arrays) | Accepted | Memory | Cut RAM per request |
| 2026-02-12 | [ADR-007: One shared browser, one render at a time](#adr-007-one-shared-browser-one-render-at-a-time) | Accepted (after 4 flips) | Concurrency | OOM on 1.9 GB instances |
| 2026-04-02 | [ADR-008: Memory budget for a 1.9 GB instance](#adr-008-memory-budget-for-a-19-gb-instance) | Accepted, partly ineffective | Memory / GC / EB | 2026-04 memory crisis |
| 2026-04-22 | [ADR-009: Recycle Chrome on a schedule and by count](#adr-009-recycle-chrome-on-a-schedule-and-by-count) | Accepted | Chrome lifecycle | Long-lived Chrome leaks memory |
| 2026-06-02 | [ADR-010: Move every "VM" template into this service](#adr-010-move-every-vm-template-into-this-service) | Accepted | Scope | Not recorded |
| 2026-09-09 | [ADR-011: Payloads are untrusted — missing keys render empty, never throw](#adr-011-payloads-are-untrusted--missing-keys-render-empty-never-throw) | Accepted | Templates / models | Migrated templates crashed on missing keys |
| 2026-10-27 | [ADR-012: Pin Chrome 154 and install it from the repo](#adr-012-pin-chrome-154-and-install-it-from-the-repo) | Accepted | Deploy / Chrome | Newer Chrome broke `--single-process` in PROD |
| 2026-10-07 | [ADR-013: A Chrome launch failure must not stop the service](#adr-013-a-chrome-launch-failure-must-not-stop-the-service) | Accepted | Startup | Service did not come up when Chrome failed |

---

## ADR-001: PuppeteerSharp + headless Chrome instead of IronPdf

### Status
Accepted

### Context & Problem Statement
The project started on 2025-01-24 (`be308a8`) with `IronPdf` 2025.1.1 as the HTML→PDF engine. <!-- verify:ignore -->

### Decision & Implementation
`4ae0477` (2025-02-18, "Working on Puppeteer pdf") added `PuppeteerSharp`, and `148d570` (2025-02-25, "Remove Iron Pdf Code") removed IronPdf. `Services/PdfService.cs:GetBrowserAsync` launches a system Chrome with a fixed `ExecutablePath`.

### Supporting Evidence & Past Incidents
- `c4ebf5b` 2025-02-24 "Working on design fixes on new lib".

### Consequences & Trade-offs
- **Positive:** no commercial PDF library; full Chrome CSS support.
- **Negative / Constraints:** the service owns a Chrome process, which is behind most later incidents (ADR-007/008/009/012). The Chrome path is code, not config (see [DEVELOPMENT](DEVELOPMENT.md#step-by-step-setup)).

### Superseded / Rejected Alternatives
1. **IronPdf:** removed. Reason not recorded (licence cost is plausible but unverified). `Downloads/Readme.md` still shows the IronPdf `pdf.SaveAs` snippet. <!-- verify:ignore -->

---

## ADR-002: RazorLight runtime templates, not compiled MVC views

### Status
Accepted

### Context & Problem Statement
Templates are `.cshtml` files under `Templates/`, rendered with a JSON-derived model at request time.

### Decision & Implementation
`RazorLight` since the first commit (`be308a8`). `GiddhTemplate.csproj` removes `Templates/**/*.cshtml` from `RazorCompile`/`RazorGenerate`, copies `Templates/**` to the output as content, and sets `PreserveCompilationContext` so RazorLight can compile at runtime.

### Consequences & Trade-offs
- **Positive:** a template change is a file change. No C# rebuild is needed for markup.
- **Negative / Constraints:** `dotnet build` never compiles templates, so template errors surface only when a request renders them. Agents must render a sample (see the [DEVELOPMENT checklist](DEVELOPMENT.md#how-an-agent-checks-its-work-verification-checklist)).

### Superseded / Rejected Alternatives
None recorded.

---

## ADR-003: Local font files only, no web fonts or CDNs

### Status
Accepted

### Context & Problem Statement
Headless Chrome on EB did not pick up fonts reliably, and templates migrated from the old system pulled Google Fonts, a remote Calibri `@font-face` and Bootstrap 3.3.7 from CDNs.

### Decision & Implementation
- `255829b` 2025-03-07 "Fix: Font in issue in headless chrome, load font directly from file as base64": font CSS is built from `Templates/Fonts/` in `Services/PdfService.cs`.
- `cae0cf3` 2025-03-12: EB installs Arabic system fonts (`.ebextensions/002_install_fonts.config`, renamed 2025-06-24).
- `c5683d8` 2026-06-06 "remove remote CDN dependencies to prevent PDF generation timeout": removed Google Fonts links and remote Calibri, replaced Bootstrap CDN with minimal local CSS, switched `GenericPdfService` from `Networkidle2` to `DOMContentLoaded` with a 15 s timeout.

### Consequences & Trade-offs
- **Positive:** renders do not depend on outbound internet, and no CDN wait stretches a render past its timeout.
- **Negative / Constraints:** never add `<link>`/`@import` to remote fonts, CSS or scripts in a template.

### Superseded / Rejected Alternatives
1. **Base64-inlined fonts:** replaced on 2026-03-02 (`a5d9644` "fix insufficient memory error") by `file://` URIs (`BuildFontCSSAsync` now emits `new Uri(file).AbsoluteUri`). Inlining every font into every page's HTML cost memory. <!-- verify:ignore -->

---

## ADR-004: Alert failures to Slack from the service itself

### Status
Accepted

### Decision & Implementation
`d379ed8` 2025-06-18 "C# Exception error message alert on slack added". `Services/SlackService.cs:SlackService` posts `{url, env, error, errorStackTrace}` to `SLACK_WEBHOOK_URL`. Controllers call it on render failure, and the global exception handler in `Program.cs:Main` calls it on unhandled exceptions (`0a36919` 2025-11-20 added route/trace context).

### Consequences & Trade-offs
- **Positive:** failures are visible without opening Grafana.
- **Negative / Constraints:** an unset webhook skips alerts silently. The service prints the webhook URL to stdout on every alert. `AccountStatementController` labels alerts from the `Environment` config key, which nothing sets.

### Superseded / Rejected Alternatives
None recorded.

---

## ADR-005: Grafana stack (Loki, Alloy, OTLP traces, Prometheus)

### Status
Accepted

### Decision & Implementation
- `ef7f949` 2025-09-04 "grafan service intergration": Grafana Alloy installed by `.ebextensions/003_add_grafana_repo.config` and configured by `.platform/hooks/postdeploy/grafana-setup.sh`.
- `568ee46` 2025-09-22 "Centralized Logging in .NET": Serilog with a GrafanaLoki sink (`appsettings.json`).
- `849453b` / `0a36919` / `c84cb52` 2025-11-20: OpenTelemetry tracing (OTLP to the local Alloy on 4318), runtime/ASP.NET metrics, and the Prometheus `/metrics` endpoint, which Alloy scrapes.
- `43463cb` 2026-08-07: `SERVICE_TYPE`/`SERVICE_NAME` EB properties as Alloy labels.

### Consequences & Trade-offs
- **Negative / Constraints:** logs reach Loki two ways, the in-process Serilog sink and Alloy tailing `/var/log/template-logs`. Inferred duplication, not measured. Telemetry contracts belong to [CONTRACTS](CONTRACTS.md). <!-- verify:ignore -->

### Superseded / Rejected Alternatives
None recorded.

---

## ADR-006: Stream PDFs through temp files, not byte arrays

### Status
Accepted

### Decision & Implementation
`3240aef` 2026-01-22 "Optimize memory usage by streaming PDFs to disk instead of loading in RAM": Chrome writes to `<tmp>/GiddhPdfs/<name>_<guid>.pdf`. The controller returns a `FileStream` with `FileOptions.DeleteOnClose`. `Services/PdfCleanupService.cs:PdfCleanupService` deletes leftovers older than 2 h every 30 min. The same commit added `Controllers/DiagnosticsController.cs` and `Services/MemoryReservationService.cs` (ADR-008).

### Consequences & Trade-offs
- **Positive:** a PDF is never fully buffered in managed memory.
- **Negative / Constraints:** the response file name is fixed (`invoice.pdf` / `document.pdf`). `5f625d9` 2026-02-24 settled on `attachment` after `bbec661` tried `inline` the same day; the reason was not recorded.

---

## ADR-007: One shared browser, one render at a time

### Status
Accepted (after four flips)

### Context & Problem Statement
One static `IBrowser` is shared by all requests. Parallel renders on a 1.9 GB instance ran out of memory.

### Decision & Implementation
Today: `Services/PdfService.cs` `_maxConcurrentPdfs = 1` ("only one Chrome render at a time"), a 90 s wait for a slot and a 2 min render budget. `GenericPdfService` and `AccountStatementPdfService` use their own semaphores. Chrome runs with `--single-process` and `--renderer-process-limit=1`.

### Supporting Evidence & Past Incidents
| Date | Sha | Change |
|---|---|---|
| 2026-02-12 | `1c8498f` | Semaphore limit 3, crash detection and recreate, production flags incl. `--single-process` |
| 2026-02-17 | `bd97436` | Limit 1, `--single-process` dropped |
| 2026-02-25 | `5fe4a26` | "swap file added and concurrent request enabled": limit 3, renderer limit 3, 4 GB swap |
| 2026-02-27 | `7c27dd0` | Back to 1 |
| 2026-06-15 | `b5ce96b` | "Reduce concurrent PDF generation to 1 and lower Chromium memory limits to prevent OOM on low-RAM environments" (Generic too) |

### Consequences & Trade-offs
- **Positive:** bounded memory.
- **Negative / Constraints:** throughput is one render per service per instance. Bursts queue for up to 90 s, then fail. Raising the limit has caused OOM twice. Do not raise it without more RAM.

### Superseded / Rejected Alternatives
1. **Concurrency 3 with swap:** tried twice (Feb 2026), reverted to 1 both times.
2. **Dispose the browser after every PDF:** `7ee34e7` 2026-02-27, removed in `a5d9644` 2026-03-02 (also attempted in `f1d0f30` "fix the cpu spike issue", reverted the same day `ae7e4aa`). Replaced by count-based recycling (ADR-009).

---

## ADR-008: Memory budget for a 1.9 GB instance

### Status
Accepted. **Partly ineffective**: Server GC is still on in published builds, see below.

### Context & Problem Statement
`MEMORY_CRISIS_RCA_AND_FIXES.md` (`cb63ccb`, 2026-04-02): after 7 days on one instance, the static Chrome had grown to about 500 MB and crashed. Re-creating it failed for lack of RAM, and the box ended at 78 MB free, 2.6 GB swap used and 99% I/O wait. Contributing settings: a 2 GB .NET heap limit on 1.9 GB RAM, Server GC, a 200 MB memory reservation in PROD, no Chrome heap cap, and no periodic Chrome restart.

### Decision & Implementation
`cb63ccb` / `c177547` 2026-04-02:
- `runtimeconfig.template.json`: `HeapHardLimit` 800 MB (was 2 GB), `System.GC.Server: false`, `ConserveMemory: 9`, `TieredCompilation: false`.
- `MemoryReservation:Enabled=false` in `appsettings.json` and `appsettings.Production.json` ("Only 1.9GB total RAM available").
- Chrome flags: `--single-process`, `--js-flags=--max-old-space-size=96 ... --max-heap-size=96`, accelerated decoding off, `VizDisplayCompositor` off.
- Kernel tuning from `bd97436` 2026-02-17 stays in place (`.ebextensions/005_memory_optimization.config`), plus 4 GB swap (`.ebextensions/swap.config`, `5fe4a26`).

### Supporting Evidence & Past Incidents
`MemoryReservation` (pre-allocating a block at startup) went back and forth:

| Date | Sha | Value |
|---|---|---|
| 2026-01-22 | `3240aef` | Added: Test 1 GB, Production 2 GB |
| 2026-02-16 | `d372504`, `1bcdfcb` | Default 1 GB → 512 MB → 200 MB |
| 2026-02-20 | `7246b66` | 1 GB everywhere. Reverted 2026-02-24 (`3b07163`) |
| 2026-03-20 | `13840c2` | "fix memory error on prod": PROD 2 GB → 200 MB, capped at `int.MaxValue` |
| 2026-04-02 | `cb63ccb` | Disabled in base and PROD. Test keeps 200 MB (`appsettings.Test.json`) |

### Consequences & Trade-offs
- **Negative / Constraints:** `Microsoft.NET.Sdk.Web` defaults `ServerGarbageCollection` to true, and that MSBuild property overrides the template. A `dotnet publish -c Release` on 2026-11-17 produced `"System.GC.Server": true`. The Workstation-GC part of this decision has therefore not been in effect. See [DEVELOPMENT](DEVELOPMENT.md#runtime-gc-settings). <!-- verify:ignore -->
- `docs/Other/ENVIRONMENT_CONFIG.md` still documents 1 GB (Test) / 2 GB (Prod) reservations. Stale.

### Superseded / Rejected Alternatives
1. **`HeapHardLimit` 512 MB with lower log retention** (`f1d0f30` 2026-03-02 "fix the cpu spike issue"): reverted the same day (`ae7e4aa`). Reason not recorded.
2. **`BrowserHealthService`** (restart Chrome every 6 h or 1000 PDFs), proposed in the RCA: never committed. `ChromeRestartService` (ADR-009) took its place. <!-- verify:ignore -->
3. **Upgrade to t3.medium (4 GB)**, recommended in the RCA: no evidence it happened.

---

## ADR-009: Recycle Chrome on a schedule and by count

### Status
Accepted

### Decision & Implementation
- `3b259b4` 2026-04-22 "Add cron to restart chrome": `Services/ChromeRestartService.cs:ChromeRestartService` relaunches Chrome daily at 09:00 IST (`Asia/Kolkata`).
- `2fba9f1` 2026-04-29 "fix chrome processes not getting delete after new chrome start": captures the old PID and kills it, with a `pkill -9 -f 'google-chrome.*headless'` fallback.
- `d770889` 2026-06-19 "fix Test - Timeout of 180000 ms exceeded on c_sharp": `PdfService` disposes the browser every 10 renders (`_browserRecycleAfter`), adds `--disk-cache-size=0`/`--media-cache-size=0`, and the controller reads `JsonElement` directly. First pushed as `f955e93`, reverted (`3e84b6e`) and re-applied the same day.

### Consequences & Trade-offs
- **Negative / Constraints:** the `pkill` pattern matches any headless Chrome on the host, not just this service's.

---

## ADR-010: Move every "VM" template into this service

### Status
Accepted

### Decision & Implementation
`86d0842` 2026-06-02 "Migrate all template form VM to Csharp" (72 files, +9,873 lines) added `Templates/other-template/` and the `POST /api/v1/GenericPdf` path (`Services/GenericPdfService.cs`), which picks a folder from `templateType`. Earlier additions to the scope: Tally (`db33c5b` 2025-04-22), Thermal (`ebb18e2` 2025-08-30), Account statement (`61d636f` 2026-01-09; customer-portal export `13c2211` 2026-01-19).

> [!TODO]
> The commit does not say what "VM" was (which system rendered these templates before) or whether it has been switched off. Needs a human.

### Consequences & Trade-offs
- An unknown `templateType` falls back to `other-template/template_a` with HTTP 200. Template catalogue: [DESIGN](DESIGN.md).

---

## ADR-011: Payloads are untrusted — missing keys render empty, never throw

### Status
Accepted

### Context & Problem Statement
Migrated templates read `dynamic` JSON. A missing key or a wrong type (a number sent as a string) threw inside Razor and failed the whole PDF.

### Decision & Implementation
- `33b4fa2` 2026-03-16: `Converters/NumberToStringConverter.cs` for the typed `Root` model, plus number fields changed to strings.
- `8c320f3` 2026-09-08: `SafeDynamicObject`/`MissingKeyProxy` in generic helpers. **Reverted the next day** (`1a57021`, "Migarate All template from VM to Csharp BUG"). <!-- verify:ignore -->
- `e0e23cd` 2026-09-09 "keep other-template PDFs rendering when payload keys or types are missing": `Services/SafeDynamic.cs:SafePayload`, `Services/SafeValue.cs`, `Services/SafeList.cs`, `Services/TemplateHelpers.cs:TemplateHelpers`, 34 templates updated, and the repo's first tests (`GiddhTemplate.Tests/SafePayloadTests.cs`).

### Consequences & Trade-offs
- **Positive:** a partial payload yields a partial PDF instead of a 500.
- **Negative / Constraints:** missing data fails silently (empty cells, zero totals). Check rendered output, not just the status code.

### Superseded / Rejected Alternatives
1. **`SafeDynamicObject` in generic helpers (`8c320f3`):** reverted for a QA bug; details not recorded.

---

## ADR-012: Pin Chrome 154 and install it from the repo

### Status
Accepted

### Context & Problem Statement
From 2025-01-25 to 2026-10-07, `.ebextensions/001_install_chromium.config` installed `google-chrome-stable_current_x86_64.rpm`, so each new EB instance got whatever Chrome was current. On 2026-10-07 PROD hit "Invoice PDF is not displaying with error 'Invoice preview is not available'" (PR #283 title). The recorded rationale (`c2be0e2`, `7b4a33c`) is to keep "a known version that works with `--single-process`".

### Decision & Implementation
`7b4a33c` 2026-10-27: `deps/google-chrome-stable-154.0.8037.97-1.x86_64.rpm.zip.partaa`/`.partab` are committed, shipped via `Content Include="deps/**"` in `GiddhTemplate.csproj`, joined and installed by `.ebextensions/001_install_chromium.config`, which blocks yum/dnf upgrades and fails the deploy on any other version.

### Supporting Evidence & Past Incidents
All on 2026-10-07 unless dated:
| Sha / PR | Change | Outcome |
|---|---|---|
| `8f73021` (#276) | Startup survives a Chrome launch failure | Kept (ADR-013) |
| `5a3d85b` (#277) | Remove `--single-process` | Undone by `b00d9a2` |
| `b00d9a2` | Pin 154 via `wget` from dl.google.com, restore `--single-process` | Kept the pin |
| `6b7a27d` (#281) | Download Chrome/Alloy from S3 | Reverted `b23f919` (#282) |
| `c2be0e2` (#283, branch `chrome-local-zip`) | RPM as one Git LFS zip in `deps/` | Reverted 2026-10-12 (`13c65a3`, #285) |
| `7b4a33c` (#290) 2026-10-27 | Two plain-git split parts | Current |

### Consequences & Trade-offs
- **Positive:** deploys need no Chrome download, and instances are reproducible.
- **Negative / Constraints:** 139 MB of binary in git history and in every build output. Chrome security updates now need a deliberate bump: new parts in `deps/`, plus `CHROME_VERSION` and the version check in `001_install_chromium.config`. Local Chrome will usually be newer than PROD's.

### Superseded / Rejected Alternatives
1. **Latest stable from Google at deploy:** caused the drift.
2. **S3 download:** reverted within the day, reason not recorded.
3. **Git LFS zip:** reverted. Reason not recorded. Inferred: the LFS pointer, not the zip, reached the EB bundle, since the replacement avoids LFS and splits below GitHub's 100 MB limit.
4. **Dropping `--single-process`:** reverted the same day.

---

## ADR-013: A Chrome launch failure must not stop the service

### Status
Accepted

### Decision & Implementation
`8f73021` 2026-10-07 "fix the issue of not getting up if the browser not launch in startup": `Program.cs:Main` wraps the pre-warm `GetBrowserAsync()` in try/catch and logs `Browser pre-warm failed`. `GetBrowserAsync` re-creates the browser on the next request.

### Consequences & Trade-offs
- **Positive:** Kestrel serves `GET /` and diagnostics even when Chrome is broken.
- **Negative / Constraints:** an instance with no working Chrome passes a plain `GET /` health check while every PDF returns 500 (observed locally without Chrome). Health checks that matter must render something.

---

## Lessons Learned & Incident Log

- **Incident / Bug:** 2026-04 PROD memory exhaustion (78 MB free, 99% I/O wait) after 7 days without a deploy.
  - **Root Cause:** a single long-lived Chrome grew to about 500 MB, crashed, and could not be re-created. The heap limit (2 GB) was larger than RAM (1.9 GB), and a memory reservation was enabled. (`MEMORY_CRISIS_RCA_AND_FIXES.md`)
  - **Prevention Rule:** ADR-007/008/009. Keep render concurrency at 1, keep Chrome heap flags, keep periodic Chrome recycling, and never enable `MemoryReservation` in PROD on this instance size.
- **Incident / Bug:** 2026-10-07 PROD "Invoice preview is not available".
  - **Root Cause:** unpinned Chrome on new EB instances. Recorded rationale: the pinned version works with `--single-process`.
  - **Prevention Rule:** ADR-012. Never install `stable_current`. Bump Chrome only deliberately, after rendering every template family on TEST. <!-- verify:ignore -->
- **Incident / Bug:** 2026-06 TEST "Timeout of 180000 ms exceeded" (`d770889`).
  - **Root Cause:** inferred from the fix: Chrome cache bloat on a long-lived browser and remote CDN assets in migrated templates (`c5683d8` a week earlier).
  - **Prevention Rule:** no remote assets in templates. Keep the disk/media cache flags and the recycle-every-10.
- **Incident / Bug:** 2026-09-08 migrated templates broke on QA after the `SafeDynamicObject` refactor, reverted next day.
  - **Root Cause:** not recorded.
  - **Prevention Rule:** ADR-011 layer plus unit tests. Render generic templates with sparse payloads before merging.
- **Pattern:** fixes are often pushed straight to `master`, reverted and re-applied within hours (`f1d0f30`/`ae7e4aa`, `f955e93`/`3e84b6e`/`d770889`, `5a3d85b`/`b00d9a2`, `6b7a27d`/`b23f919`). There is no CI and no automated render test, so TEST is the first real check.
  - **Prevention Rule:** run the [verification checklist](DEVELOPMENT.md#how-an-agent-checks-its-work-verification-checklist) locally, rendering each affected template, before opening the PR.
- **Pattern:** the Chrome `ExecutablePath` was committed pointing at a laptop path more than once (`3cbec8e` / `60b7eca` "revert chrome path changes", `7317abe` "chrome path fixes").
  - **Prevention Rule:** never commit a non-`/usr/bin/google-chrome` path. Making it configurable is an open question.
- **Stale guidance:** `MEMORY_CRISIS_RCA_AND_FIXES.md` cites `.ebextensions/01_environment.config`, `.ebextensions/02_system_config.config`, `Services/BrowserHealthService.cs`, and a systemd unit `giddh-template`. None of these exist in git history; the service runs on Elastic Beanstalk. Use [DEVELOPMENT](DEVELOPMENT.md#what-runs-on-an-eb-instance-at-deploy) for the deploy process. <!-- verify:ignore -->
