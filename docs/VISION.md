---
covers: ["*"]
verified_at: d462f69
verified_on: 2026-11-17
---
# Product Vision & Strategic Direction

GiddhTemplate (repo GIDDH_UI_Sharp) is the PDF rendering service behind Giddh, Walkover's accounting product. The Giddh backend sends it a voucher, invoice or statement as JSON. The service fills a Razor template and prints it to PDF with headless Chrome, so every customer-facing document Giddh produces looks consistent and correct. It has no UI and no data store of its own.

## Authoritative Sources & Status

| Source | What it gives |
|---|---|
| The owner's answers, 2026-11-17 | Callers (the Giddh backend only) and the ship flow: **approved** |
| `README.md`, `COMPLETE_DOCUMENTATION.md` | Purpose ("PDF Generation Service for Giddh Application"), template families, theming. Partly stale; superseded by this doc set |
| `MEMORY_CRISIS_RCA_AND_FIXES.md` | The production incident that shaped the Chrome and memory design (see [DECISIONS.md](DECISIONS.md)) |
| `GiddhTemplate.csproj`, `Program.cs`, `Controllers/` | What is actually built |
| Git history (813 commits, 2025-01-24 to 2026-11-13) | Most recent work is template styling and fidelity, plus memory and observability hardening |

- **Approved direction:** what the owner confirmed (below) and the purpose stated in `README.md`.
- **Current implementation:** described in [ARCHITECTURE.md](ARCHITECTURE.md), [FLOWS.md](FLOWS.md) and [CONTRACTS.md](CONTRACTS.md).
- **Proposals:** there is no roadmap in the repo. Treat git history as a change log, not a plan.
- **Unknowns:** see [OPEN_QUESTIONS.md](OPEN_QUESTIONS.md).

### Approved Direction

Confirmed by the owner, 2026-11-17:
- **The only caller is the Giddh backend,** server to server. No browser or frontend calls this service directly.
- **Ship flow:** a PR into `master` deploys to the **test** Elastic Beanstalk environment; `master` is then merged into `production`, which deploys to **prod**.

Stated in the repo:
- This is the PDF generation service for the Giddh application (`README.md`, `COMPLETE_DOCUMENTATION.md`).
- It renders three main template families (TemplateA, Tally, Thermal) plus account statements and a generic named-template path. The catalogue is in [DESIGN.md](DESIGN.md).

## Target Audience & Ideal Customer Profile (ICP)

- **Direct consumer:** the Giddh backend, which builds the payload and stores or returns the PDF (confirmed by the owner, 2026-11-17).
- **People who see the output:** Giddh customers, meaning businesses and their own customers who receive invoices, vouchers, purchase orders and statements. Only the PDF matters to them; how it is made does not. This is inferred from the document types the templates render.
- **People who change this repo:** Giddh engineers. In practice most changes are template layout, styling and fonts (inferred from git history).

## Problems Solved & Value Proposition

| Customer Problem | How We Solve It | Unique Value / Differentiator |
|---|---|---|
| Giddh needs pixel-accurate, print-ready accounting documents | Razor templates rendered by real Chrome and printed to PDF | The HTML and CSS look the same in a browser and in the PDF |
| Different businesses want different document styles | Template families and per-request theme options (fonts, colours, margins) | One service covers A4 invoices, Tally-style layouts and thermal receipts |
| Backend payloads are large and not always complete | `Services/SafeDynamic.cs`, `Services/SafeList.cs` and `Services/SafeValue.cs` tolerate missing or wrong-typed fields | A bad field degrades one value, not the whole document |
| Rendering at volume exhausted memory in production | Chrome lifecycle management, memory reservation and cleanup services | It survives long uptimes on small instances (see the incident in [DECISIONS.md](DECISIONS.md)) |

## Core Principles & Design Philosophy

These are inferred from the code and history. An owner has not approved them.

1. **The backend decides and this service draws.** Totals, taxes and every business number arrive computed in the payload. A template formats values; it does not recalculate them. *Trade-off:* changing a number means changing the backend.
2. **Never fail a document over one bad field.** Read payload data through the safe wrappers. *Trade-off:* a missing value prints blank instead of raising an error, so watch the logs.
3. **Output fidelity is the product.** Most commits are visual fixes. Check any template change by rendering a sample payload and looking at the PDF.
4. **Stay alive on small instances.** Chrome is long-lived but restarted on a schedule, and memory is reserved and monitored. Don't add work that holds large buffers or browser pages.
5. **Observable by default.** Unhandled errors go to the logs (Loki) and Slack, and metrics go to Prometheus.

## Strategic Priorities & Success Measures

> [!TODO]
> No priorities or success metrics are written down. History suggests the focus is template fidelity and runtime stability. Measures such as error rate, p95 render time and memory headroom would need an owner to set them (see [OPEN_QUESTIONS.md](OPEN_QUESTIONS.md)).

## Boundaries & Non-Goals

- **Not a public API.** Only the Giddh backend calls it. Never add browser-facing endpoints, CORS for frontends, or end-user auth flows without an owner decision.
- **Not a calculation engine.** No tax, totals or currency-conversion logic belongs here beyond formatting what the payload carries.
- **Not a data store.** It keeps no records. Generated files are temporary and cleaned up.
- **Not a general-purpose HTML-to-PDF service** for other Walkover products, unless the owner says otherwise.

**Anti-patterns:**
- Reading payload fields without the safe wrappers.
- Hard-coding one customer's data or layout into a shared template.
- Holding Chrome pages open, or loading whole files into memory, without cleanup.
- Weakening the restart, memory or cleanup services to "fix" latency.

## How Future Agents Must Evaluate Requests

1. **Align with approved direction:** compare the request to the callers, boundaries and principles above.
2. **Improve unclear requests:** for a template change, ask for or pick a sample payload and the exact document type, template and voucher type it affects before editing.
3. **Flag conflicts:** if a request needs calculation, persistence or a new public caller, explain the conflict and recommend that the backend do it instead.
4. **Explicit direction changes:** when the owner deliberately widens scope (for example a new caller), update this file in the same change.
5. **No fabricated integrations:** if a feature needs data the backend doesn't send, write the payload contract it needs in [CONTRACTS.md](CONTRACTS.md). Never invent fields in sample payloads to make a template look finished.
