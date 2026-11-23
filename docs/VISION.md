---
covers: ["*"]
verified_at: ba2323b
verified_on: 2026-11-23
---

# Product Vision & Strategic Direction

**Giddh Template Service** (repository `GIDDH_UI_Sharp`, project name `GiddhTemplate`) is a backend PDF rendering service for the **Giddh** accounting product (Walkover Web Solution). It turns structured JSON from Giddh into print-ready PDFs using Razor HTML templates and headless Chrome (PuppeteerSharp). It is not the Giddh web application; it exists so vouchers, statements, ledgers, thermal receipts, and related documents render consistently for Indian accounting workflows (including GST- and Tally-oriented layouts).

## Authoritative Sources & Status

**Sources used (read at `verified_at`):**

| Source | Role |
|--------|------|
| `README.md` | Product summary, stack, primary API, template families |
| `COMPLETE_DOCUMENTATION.md`, `DEVELOPER_GUIDE.md`, `docs/*.md` | Workflows, payload capture from Giddh, ops and architecture |
| `Program.cs`, `Controllers/`, `Services/` | Runtime behavior, endpoints, observability |
| `Templates/` | Supported document types and presentation |
| `MEMORY_CRISIS_RCA_AND_FIXES.md`, `docs/Other/DISK_STREAMING_IMPLEMENTATION.md` | Reliability and resource constraints |
| Recent `git log` (e.g. certificate feature, Chrome pinning, template styling) | In-flight product additions |

**No `AGENTS.md` or root `VISION.md` was present when this draft was written.**

### Approved direction

- Serve **Giddh** as a dedicated **PDF generation microservice**: HTTP API in, PDF bytes (streamed) out.
- Support **multiple document templates** (invoice/voucher families, Tally, thermal, ledgers, reports, email-oriented layouts, certificates) driven by **JSON payloads** aligned with Giddh data.
- Deploy as a **production-grade API** on constrained infrastructure (documented AWS Elastic Beanstalk / `.ebextensions`, bundled Chrome), with **structured logging**, **OpenTelemetry**, **Prometheus**, and **Slack error alerts**.
- Preserve **high visual fidelity** by rendering HTML/CSS via Chrome rather than abandoning template richness.

### Current implementation

- **.NET 8** ASP.NET Core service (`GiddhTemplate.csproj`, `Program.cs`).
- **Endpoints (non-exhaustive):** `GET /` health string; `POST /api/v1/pdf` (typed `Root` invoice/voucher payload); `POST /api/v1/genericpdf` (dynamic `templateType` with large `other-template` catalog); `POST /api/v1/account-statement`; `POST /api/v1/certificate`; `GET api/diagnostics/*` for memory/GC/health.
- **Rendering pipeline:** RazorLight templates under `Templates/` → PuppeteerSharp PDF → **temp files on disk** streamed with `FileOptions.DeleteOnClose` (see `PdfController`, `GenericPdfController`).
- **Concurrency model:** Shared Chrome browser with restart/cleanup hosted services (`ChromeRestartService`, `PdfCleanupService`, `MemoryReservationService`); generic path serializes PDF generation (semaphore) for low-RAM safety.
- **Observability:** Serilog (console, file, Loki sink in packages), OTLP traces, Prometheus scrape endpoint.
- **Integration pattern (documented):** Developers capture JSON from Giddh (`books.giddh.com` / `test.giddh.com`) via `download-file` flows; JWT auth is described at the **Giddh API** layer, not enforced in this service’s codebase.
- **Tests:** `GiddhTemplate.Tests` (limited; e.g. safe payload tests).

### Proposals & roadmaps

- **Memory/chrome stability work** documented in `MEMORY_CRISIS_RCA_AND_FIXES.md` (browser lifecycle, GC/heap tuning, deployment follow-up). Treat as operational roadmap unless owners mark it complete.
- **Certificate PDFs** appear from recent commits (`CertificateController`); product positioning not stated in README.
- **Ongoing template work** (GST variants, ledger views, styling) visible in `Templates/other-template/` and merge history—implementation-led expansion without a written roadmap file.

### Unknowns & ambiguities

See **`docs/OPEN_QUESTIONS.md`**. Do not infer answers from code alone (e.g. anonymous API access may be environmental/network trust, not an intentional public API).

---

## Target Audience & Ideal Customer Profile (ICP)

### Primary persona

- **Giddh backend or platform engineer** (or integrator on the Giddh stack): sends JSON produced by Giddh voucher/statement flows to this service and returns PDFs to end users inside Giddh.
- **Workflow:** Configure template + payload → `POST` API → stream PDF to user download/preview in Giddh.
- **Technical level:** Comfortable with REST, JSON schema shaped like `Root` / template-specific payloads, Chrome/Puppeteer ops on Linux servers.

### Secondary personas

- **Giddh template/CSS author:** edits Razor and CSS under `Templates/` for branding, GST fields, and layout flags driven by payload `settings`.
- **SRE / DevOps (Walkover):** owns EB deploys, Chrome pinning (`deps/`), memory/diagnostics, Slack/Loki/Grafana wiring via environment variables (`DEVELOPER_GUIDE.md`).
- **QA / support:** reproduces PDF issues using payloads from test.giddh.com and `sample-payloads/` (per `docs/pdf-generation.md`).
- **Auditors / compliance stakeholders (indirect):** depend on accurate tax and ledger presentation in generated PDFs; they do not call this API directly.

---

## Problems Solved & Value Proposition

| Customer problem | How this product solves it | What sets it apart |
|------------------|---------------------------|-------------------|
| Giddh users need downloadable/printable invoices, receipts, and statements that match on-screen data | Server-side render of Giddh JSON into PDF via templates tuned for Giddh voucher types | Purpose-built for Giddh payload shapes and Indian accounting layouts (GST, Tally, thermal) |
| Branding and layout vary by company and template | Razor + CSS theming (fonts, colors, margins, conditional columns via settings) | Same engine serves many template families without shipping a desktop print stack to users |
| PDF preview must match complex HTML (tables, multilingual labels, QR/signatures per docs) | Headless Chrome PDF export | Fidelity trade-off accepted vs. lightweight PDF libraries |
| Main app must stay responsive under PDF load | Dedicated microservice isolates CPU/RAM-heavy Chrome work | Clear boundary: Giddh app orchestrates; this service renders |
| Production incidents must be visible quickly | Structured logs, traces, Prometheus, Slack on failures | Ops-oriented defaults in `Program.cs` |

---

## Core Principles & Design Philosophy

1. **Template fidelity over minimal footprint** — Use HTML/CSS + Chrome for output that matches business documents. *Trade-off:* higher RAM/CPU and operational complexity than library-based PDFs.

2. **Payload-driven rendering, not business logic** — Accept structured JSON; compute presentation in templates/services, not full accounting rules. *Trade-off:* tight coupling to Giddh payload evolution; schema drift breaks templates.

3. **Survive small instances** — Prefer disk streaming, bounded concurrency, browser restart, and cleanup over holding large buffers or unbounded parallelism. *Trade-off:* lower peak throughput and possible queueing/timeouts under load.

4. **Observable failures** — Log with trace correlation; alert humans on generation errors (Slack). *Trade-off:* noise if thresholds are wrong; requires webhook and log pipeline configuration.

5. **Templates as product surface** — New document types should land as versioned Razor/CSS assets with explicit `templateType` (or typed models), not one-off hardcoded PDF code. *Trade-off:* growing `Templates/other-template` maintenance burden.

6. **Stateless document generation** — Generate on demand; do not position this service as document storage. *Trade-off:* callers must retain vouchers; no “list my PDFs” from this API alone.

---

## Strategic Priorities & Success Measures

**Stated priorities (from docs and implementation emphasis):**

- Reliable PDF generation for Giddh production traffic on **Linux + bundled Chrome**.
- **Memory-stable** long-running instances (post–memory-crisis fixes and streaming architecture).
- **Template coverage** for Giddh document types (invoices, ledgers, thermal, Tally, reports, certificates in code).

**Success metrics:** Not stated in repository sources. Operational signals exist (Prometheus, diagnostics memory endpoint, slow-request logging &gt; 5s in `Program.cs`) but not product SLOs—see open questions.

---

## Boundaries & Non-Goals

**Deliberate scope limits (stated or strongly implied):**

- **Not** the Giddh web UI, user login, or `books.giddh.com` application (`README.md` links Giddh as a separate related project).
- **Not** a general-purpose PDF SaaS for arbitrary HTML uploads; inputs are **Giddh-shaped JSON** and known templates.
- **Not** system of record for accounting data, payments, or e-invoicing government submission—rendering only.
- **Not** a long-term document archive; temporary files and delete-on-close streaming are the norm.

**Anti-patterns (break the vision):**

- Adding **accounting/tax calculation** that should live in Giddh core APIs.
- **Bypassing templates** with large procedural PDF drawing for standard vouchers (fragments the theming model).
- **Unbounded concurrency** of Chrome renders on memory-constrained EB instances.
- **Fabricating mock Giddh APIs** in this repo instead of documenting real contracts in `docs/CONTRACTS.md`.
- Treating **anonymous diagnostics/GC endpoints** as safe on the public internet without an owner decision.

---

## How Future Agents Must Evaluate Requests

1. **Compare the request** to *Approved direction*, *ICP*, *Core principles*, and *Boundaries* above—not to the request’s own marketing language.

2. **Improve unclear requests** before coding: identify which endpoint family (`/pdf`, `/genericpdf`, account-statement, certificate) and which template/payload contract applies.

3. **Explain conflicts** plainly: if a request would turn this into a public multi-tenant PDF platform, duplicate Giddh business logic, or ignore memory/Chrome constraints, recommend declining or scoping down.

4. **Deliberate direction changes** require an explicit owner decision and an update to this file in the same change set (brief addition, do not dilute boundaries to appease a request).

5. **Backend dependencies:** If a feature needs capabilities from Giddh API or another service that are not implemented here, describe the contract in **`docs/CONTRACTS.md`** (create or extend). Do not add fake endpoints or pretend integrations exist.

6. **Separate evidence types:** Label whether behavior is *approved*, *implemented today*, *proposed*, or *unknown*; add unknowns to `docs/OPEN_QUESTIONS.md` when owners must decide.

7. **Regressions on reliability:** Changes that increase RAM per request or Chrome parallelism demand scrutiny against disk-streaming and concurrency patterns documented in `docs/Other/DISK_STREAMING_IMPLEMENTATION.md` and memory guides.

---

*Draft vision document. An owner must review and approve before merge; this file gates which follow-up requests agents should implement.*