---
covers: ["Controllers/**", "Models/**", "Converters/**", "sample-payloads/**", "GiddhTemplate.http", "Services/SafeDynamic.cs", "Services/SafeList.cs", "Services/SafeValue.cs", "Services/SlackService.cs", "Services/ISlackService.cs"]
verified_at: d462f69
verified_on: 2026-11-17
---
# Contracts & Data Schemas

What crosses this service's edges: the HTTP routes the Giddh backend calls, the payload models those
routes bind, the tolerance rules for schema-less payloads, and the observability outputs (logs, traces,
metrics, alerts). The C# types are the source of truth; this page names them and states the rules that
are not visible from the types alone. Do not copy type definitions here.

The only caller is the Giddh backend, server to server; no browser calls this service (confirmed by the
owner, 2026-11-17). There is no frontend in this repo, so the template's client-cache section is replaced
by a one-line note below.

## Authoritative Type Definitions

| Domain Entity | Primary TS Type / Interface | Source File | Description |
|---|---|---|---|
| Invoice / voucher payload | `InvoiceData.Root` (C#) | `Models/RequestModel.cs:Root` | Strongly typed body of `POST /api/v1/pdf`. Every property is nullable. |
| Label settings | `Settings`, `Setting` | `Models/RequestModel.cs:Settings` | One `Setting` (`Label`, `SecondaryLabel`, `Display`) per printable field, keyed by camelCase name under `settings`. |
| Bilingual label config | `LabelDisplayConfig` | `Models/RequestModel.cs:LabelDisplayConfig` | Computed from top-level `displayLanguage1/2`, `enableSecondaryLanguage`, `showLanguage2DisplayedFirst`, `language1Code/2Code`, `separator` via `Root.LabelDisplay`. |
| Label rendering | `LabelRenderer` | `Models/LabelRenderer.cs:LabelRenderer` | Turns a `Setting` + `LabelDisplayConfig` into text/HTML; RTL `lang`/`dir` for `ar`, `fa`, `he`, `ur`, etc. |
| Voucher type names | `VoucherTypeEnums` | `Models/Enums/Common.enum.cs:VoucherTypeEnums` | Display strings ("Purchase Bill", "Receipt"…) read via `GetVoucherTypeEnumValue` to pick a TemplateA layout. |
| Account statement payload | `GiddhTemplate.Models.Root` | `Models/AccountStatementRequest.cs:Root` | Body of the account-statement route. Same class name as the invoice `Root`, different namespace. |
| Generic payload | none (schema-less `JsonElement`) | `Services/SafeDynamic.cs:SafePayload` | Body of `POST /api/v1/genericpdf`, wrapped into safe dynamic values (see [Payload tolerance](#payload-tolerance-contract-generic-route-only)). |
| Number-as-string | `NumberToStringConverter` | `Converters/NumberToStringConverter.cs:NumberToStringConverter` | Lets a JSON number bind to a `string?` property on the invoice route only. |
| Slack alert | `ISlackService.SendErrorAlertAsync` | `Services/ISlackService.cs:ISlackService` | Alert payload, see [Alerts](#slack-alert-contract). |

> [!WARNING]
> Two classes are named `Root`: `InvoiceData.Root` (`Models/RequestModel.cs`) and
> `GiddhTemplate.Models.Root` (`Models/AccountStatementRequest.cs`). `Controllers/AccountStatementController.cs`
> uses the fully-qualified name for this reason. Keep it that way when editing either controller.

### Fields that choose the template

| Route | Field (JSON) | Effect | Code |
|---|---|---|---|
| `POST /api/v1/pdf` | `templateType` (case-insensitive) | `TALLY` → `Templates/Tally/`, `THERMAL` → `Templates/Thermal/`, anything else or absent → `Templates/TemplateA/` | `Services/PdfService.cs:GeneratePdfToFileAsync` |
| `POST /api/v1/pdf` | `voucherType` (TemplateA only, case-insensitive) | `Receipt`/`Payment` → `Receipt_Payment_Body.cshtml` (body only); `Purchase Order`/`Purchase Bill` → `PO_PB_Header.cshtml` + `PO_PB_Body.cshtml`; else `Header`/`Body`/`Footer.cshtml` | same |
| `POST /api/v1/pdf` | `theme.margin.{top,right,bottom,left}` | Page margins in px, floored at top 10 / right 10 / bottom 15 / left 10 | same |
| `POST /api/v1/genericpdf` | `templateType` (key is case-sensitive, value is upper-cased) | Mapped by a fixed switch to a folder under `Templates/other-template/`; unknown → `other-template/template_a`. Each folder renders its `Template.cshtml`. | `Services/GenericPdfService.cs:GeneratePdfToFileAsync` |
| account statement | none | Always `Templates/AccountStatement/body.cshtml`; `templateType` is bound but ignored | `Services/AccountStatementPdfService.cs:GenerateAccountStatementPdfToFileAsync` |
| all PDF routes | `pdfRename` (`PdfRename`) | Only names the temp file (and the dev copy in `Downloads/`). The response filename is fixed per route. | `Services/PdfService.cs:GeneratePdfToFileAsync` |

The full list of generic `templateType` values and their folders is the switch in
`Services/GenericPdfService.cs`; the template catalogue itself is owned by [DESIGN](DESIGN.md).

> [!WARNING]
> On `POST /api/v1/genericpdf`, `templateType` values `TALLY` and `THERMAL` map to `Templates/Tally/` and
> `Templates/Thermal/`, which have no `Template.cshtml`. The request fails with `FileNotFoundException`
> and returns 500. Send those to `POST /api/v1/pdf` instead. The key must be spelled exactly
> `templateType`: `TemplateType` is not found (`JsonElement.TryGetProperty` is case-sensitive) and the
> request silently falls back to `other-template/template_a`.

### Sample payloads

| File | Route | Template it exercises |
|---|---|---|
| `sample-payloads/TemplateA-Complete-Sample.json` | `POST /api/v1/pdf` | `TemplateA` (`voucherType` `INVOICE` → Header/Body/Footer) |
| `sample-payloads/Tally-Complete-Sample.json` | `POST /api/v1/pdf` | `Tally` |
| `sample-payloads/Thermal-Complete-Sample.json` | `POST /api/v1/pdf` | `Thermal` |
| `sample-payloads/MultiLanguage-Labels-Sample.json` | `POST /api/v1/pdf` | `Tally` with a secondary-language (Hindi) `settings` block |
| `Templates/other-template/<folder>/<folder>.csharp.json` | `POST /api/v1/genericpdf` | One per generic template; its `templateType` is the folder name, upper-cased by the service |

All four `sample-payloads/*.json` deserialize into `InvoiceData.Root` with the route's options and carry
a non-empty `company.name` (checked 2026-11-17 with a scratch program outside the repo). There is no
account-statement sample. `GiddhTemplate.http` is the scaffold default: it calls `GET /weatherforecast/`,
which does not exist here.<!-- verify:ignore -->

## Backend API Endpoints & Contracts

Kestrel listens on `http://localhost:5000` (`appsettings.json` `Kestrel:Endpoints:Http:Url`); on Elastic
Beanstalk nginx proxies port 80 `location /` to it (`.platform/nginx/cond.f/elasticbeanstalk/00_application.conf`).
Routes come from attribute routing in `Controllers/`; route matching is case-insensitive.

| Method | Endpoint Path | Auth Required | Request Type | Response Type | Error Statuses |
|---|---|---|---|---|---|
| GET | `/` | None | none | 200 `text/plain` "Hello from Giddh template!" (`Controllers/PdfController.cs:MainController`) | none |
| POST | `/api/v1/pdf` | None | JSON → `InvoiceData.Root` | 200 `application/pdf`, file stream, `Content-Disposition: attachment; filename=invoice.pdf` | 400, 500 |
| POST | `/api/v1/genericpdf` | None | any JSON object (`JsonElement`) | 200 `application/pdf`, file stream, `filename=document.pdf` | 400, 500 |
| POST | `/api/v1/account-statement` (alias `/api/v1/accountstatement`) | None | JSON → `GiddhTemplate.Models.Root` | 200 `application/pdf`, file stream, `filename=account-statement.pdf` | 400, 500 | <!-- verify:ignore -->
| GET | `/api/diagnostics/health` | None | none | 200 JSON `{status, timestamp, service, version}`, constant | none | <!-- verify:ignore -->
| GET | `/api/diagnostics/memory` | None | none | 200 JSON: process/GC memory, GC counts, thread/handle counts, uptime | none |
| POST | `/api/diagnostics/gc` | None | none | 200 JSON: forces a full blocking GC, returns MB before/after/freed | none | <!-- verify:ignore -->
| GET | `/metrics` | None | none | Prometheus text exposition (`Program.cs:Main` → `MapPrometheusScrapingEndpoint`) | none |

Response bodies are always streamed from a temp PDF on disk (`FileOptions.DeleteOnClose`), never a byte
array; the old docs' `GeneratePdfAsync` returning `byte[]` is gone. Diagnostics JSON uses ASP.NET's default
camelCase. No route sets custom response headers.

### `POST /api/v1/pdf`
Handler `Controllers/PdfController.cs:GeneratePdfAsync`. Body binds as `JsonElement`, then
`Deserialize<Root>` with `PropertyNameCaseInsensitive = true` and `NumberToStringConverter`.

- **400** `{"error":"Invalid request data. Ensure payload matches expected format."}` when the body is
  `null` or `company.name` is null/empty. This is the only required field.
- **500** `{"error":"Failed to generate PDF!"}` plus a Slack alert when the service returns no file.
- **500** global envelope (below) for everything thrown: type mismatches during deserialization, Chrome
  crash, render timeout (2 min), or the render slot not freeing within 90 s ("service is busy").

> [!WARNING]
> The invoice route is **not** payload-tolerant. Observed with a scratch deserialization of
> `InvoiceData.Root`: a JSON `true` in a string field (`voucherNumber`), a JSON string in a number field
> (`exchangeRate: "1.5"`) and `voucherTypeEnums: "Purchase Bill"` all throw a JSON exception → 500 + Slack
> alert, not 400. `VoucherTypeEnums` binds only by member name (`"PurchaseBill"`); System.Text.Json ignores
> `[EnumMember]`. Unknown JSON keys are ignored. Numbers into `string?` properties are accepted.

> [!NOTE]
> `Settings.InitializeStaticKeys` (map `SettingDetails` onto the typed `Setting` properties) is never called.
> The backend sends each setting directly under `settings` by name, e.g.
> `"settings": {"taxableAmount": {"label": "…", "secondaryLabel": "…", "display": true}}`.

### `POST /api/v1/genericpdf`
Handler `Controllers/GenericPdfController.cs:GeneratePdfAsync`. The body stays a `JsonElement` and is
rendered through the safe model.

- **400** plain-text `Request body cannot be empty.` when the body is JSON `null`.
- **500** `{"error":"Failed to generate PDF!"}` + Slack when no file is produced.
- **500** `{"error":"PDF generation failed","details":"<exception message>"}` + Slack for any exception
  (missing template folder or `Template.cshtml`, busy timeout, Chrome failure). This route catches its own
  exceptions, so the global handler does not run. The alert's `url` field says `api/v1/pdf` (copy-paste
  in the controller), not `api/v1/genericpdf`.
- **200 with a placeholder PDF** when the Razor render itself throws:
  `Services/GenericPdfService.cs:BuildFallbackHtml` prints "Document could not be rendered completely" and
  the template folder name. No alert is sent; only a console line records it.

### `POST /api/v1/account-statement`
Handler `Controllers/AccountStatementController.cs:GenerateAccountStatementPdfAsync`. Body binds as
`object`, is re-serialized, then deserialized case-insensitively into `GiddhTemplate.Models.Root` (no
number-to-string converter).

- **400** plain-text `Invalid request data. Ensure payload matches expected format.` when `accountName`
  is empty.
- **500** `{"error":"Failed to generate account statement PDF!"}` + Slack when no file is produced.
- **500** global envelope for thrown errors. Amounts are non-nullable `decimal`; a string amount throws.
  The render slot is awaited with no timeout (unlike the other two routes).

### Limits a caller hits before the code does

| Limit | Value | Where | Caller sees |
|---|---|---|---|
| Request body size | 5 MB | `.platform/nginx/nginx.conf` `client_max_body_size` | 413 from nginx. Base64 logos, signatures and QR codes count. |
| Upstream timeout | 90 s, or 60 s if not loaded | `.platform/nginx/cond.f/elasticbeanstalk/00_application.conf` `proxy_read_timeout` (if that file is loaded: the folder is named cond.f, not conf.d, so EB may apply the 60 s default instead; see [Q-004](OPEN_QUESTIONS.md)) | 504 from nginx, while the service may still render (90 s slot wait + up to 2 min render) |
| Concurrency | 1 render per route type | `Services/PdfService.cs`, `Services/GenericPdfService.cs`, `Services/AccountStatementPdfService.cs` (`_pdfGenerationSemaphore`) | Queued; after 90 s a 500 (invoice: global envelope; generic: `details` = "service is busy") |

Busy and timeout cases return 500, not 429/503, so the caller cannot tell "retry later" from "bad
payload" by status code. Retry behaviour on the Giddh side is not in this repo.

## Authentication & Authorization Assumptions

- **None in code.** No `[Authorize]`, no authentication middleware, no API key or header check, no CORS
  policy (`Program.cs:Main`). `AllowedHosts` is `*` (`appsettings.json`).
- **Network isolation (inference).** The service appears to rely on not being reachable except from the
  Giddh backend. Nothing in the repo shows it: nginx forwards every path on port 80, including `/metrics`
  and `POST /api/diagnostics/gc`. <!-- verify:ignore -->
- **Tenant isolation:** none; every request is self-contained and nothing is stored after the response.

> [!TODO]
> Not determinable from the repository — no security-group, VPC, load-balancer or EB environment
> settings are committed under `.ebextensions/` or `.platform/`. Needs a human to state how the
> Elastic Beanstalk environments are kept private, and whether `/metrics` and `/api/diagnostics/*` should <!-- verify:ignore -->
> be reachable through nginx at all.

## Standard Error Envelope

There is no single envelope. What a caller receives depends on the route and the failure:

| Source | Status | Content type | Body |
|---|---|---|---|
| Global handler (`Program.cs:Main`, `UseExceptionHandler`): any exception not caught in a controller | 500 | `application/json` | see below |
| Controller "no file produced" | 500 | `application/json` | `{"error": "<route-specific message>"}` |
| `GenericPdfController` catch | 500 | `application/json` | `{"error": "PDF generation failed", "details": "<exception message>"}` |
| `PdfController` validation | 400 | `application/json` | `{"error": "Invalid request data. …"}` |
| Generic / account-statement validation | 400 | `text/plain` | bare sentence |
| Malformed JSON or empty body (inferred: `[ApiController]` model validation, not customised) | 400 | `application/problem+json` | ASP.NET validation problem details |

```json
{
  "error": "Internal Server Error",
  "traceId": "0HN…:00000001",
  "timestamp": "2026-11-17T10:00:00+00:00"
}
```

`traceId` is the request's `TraceIdentifier` (Kestrel connection id), not the W3C trace id that the log line
and Slack alert carry. To correlate a caller's 500 with Loki/Tempo, search by time and route.

## Payload tolerance contract (generic route only)

Generic templates are rendered against `SafePayload.From(model)` (`Services/GenericRazorTemplateService.cs`
calls it on every render). The Giddh backend sends these payloads with no schema, so a template must never
throw because a key is missing, null or of an unexpected type. Introduced in `e0e23cd` ("keep
other-template PDFs rendering when payload keys or types are missing"); pinned by
`GiddhTemplate.Tests/SafePayloadTests.cs`.

| JSON input | Becomes | Defined in |
|---|---|---|
| object | `SafeDynamicObject` | `Services/SafeDynamic.cs:SafeDynamicObject` |
| array | `SafeList` | `Services/SafeList.cs:SafeList` |
| string / number / bool | `SafeValue` (numbers become `decimal`) | `Services/SafeValue.cs:SafeValue` |
| `null`, absent key | `SafeMissing.Instance` | `Services/SafeDynamic.cs:SafeMissing` |

Rules a template author can rely on:

- **Missing or null key:** model.a.b.c never throws; each step yields `SafeMissing`. It prints as `""`,
  converts to `0`/`false`/`""`, enumerates as empty, `== null` is `true`, arithmetic treats it as `0`.
- **Key casing:** exact match first, then case-insensitive (`SafeDynamicObject.Lookup`).
- **Wrong container type:** `foreach` over an object (where a list was expected) is a no-op; indexing a
  list out of range returns `SafeMissing`; `.Count`/`.Length` work on lists (and are `0` on scalars).
- **Wrong scalar type:** casts never throw. `"1,234.50"` → `1234.50m` (commas and NBSP stripped,
  invariant culture); unparsable text → `0`; `"yes"`/`"y"`/`"on"`/non-zero → `true`. Division or modulo
  by zero → `0`.
- **Equality across types:** `"18" == 18` and `"true" == true` compare by value (`SafeValue.AreEqual`).
- **`+`:** numeric if both sides look numeric, string concatenation if either is non-numeric text.
- **Method calls:** `Split`, `Contains`, `StartsWith`, `EndsWith`, `Trim`, `ToString`, `Equals` are handled
  on strings; other methods are reflected onto the raw value; any failure returns `SafeMissing`.
- **Helpers:** templates get `Has`, `Get`, `Str`, `StrOr`, `Dec`, `Int`, `Bool`, `Items`, `CountOf`,
  `ShowCol`, `ColLbl`, `IsEmpty`, `Split`, `Try` from `Services/TemplateHelpers.cs:TemplateHelpers`,
  auto-imported by `Services/GenericRazorTemplateService.cs:GenericRazorTemplateService`.

> [!WARNING]
> Tolerance hides data problems: a misspelled key renders as blank or zero, and a template that still
> throws produces a 200 placeholder PDF (above). Nothing tells the backend. When a generic PDF "looks
> empty", check the payload keys against the template before touching the safe types.
> `SafeValue.ToString` formats numbers with the current culture; `SafeConvert.ToText` uses invariant
> culture.

## Persistence & Client Cache Contracts

None: the service has no database, cache or client storage. Temp PDFs live in `<temp>/GiddhPdfs` only
until the response stream closes (lifecycle owned by [ARCHITECTURE](ARCHITECTURE.md)).

## Observability contracts

The service emits no product analytics events. These are the outputs other systems consume.
Configuration keys and env vars are listed by name; values and setup belong to [DEVELOPMENT](DEVELOPMENT.md).

### Metrics (Prometheus)
- `GET /metrics` on port 5000, from `AddPrometheusExporter` in `Program.cs:Main`. Instruments: ASP.NET Core
  request metrics and .NET runtime metrics.
- Meter `GiddhTemplate.Metrics` is subscribed, but no code creates it, so no custom metrics exist.
- Scraped every 15 s by Grafana Alloy as job `giddh-template-app`
  (`.platform/hooks/postdeploy/grafana-setup.sh`, `app_metrics_scrape`) and remote-written with labels
  `env`, `server_region`, `company`, `product`, `service_name`, `service_type`, `instance`.

### Traces (OpenTelemetry)
- OTLP over HTTP/protobuf to `http://127.0.0.1:4318/v1/traces`, hard-coded in `Program.cs:Main` (the
  local Alloy receiver, which forwards to Tempo). Standard `OTEL_EXPORTER_OTLP_*` env vars are overridden.
  <!-- verify:ignore -->
- Header `X-Scope-OrgID` from env `GRAFANA_ORG_ID` when set. Sampler: always on.
- Resource: service name `SERVICE_NAME` (default `giddh-template`), version `APP_VERSION` (default `1.0.0`),
  attributes `deployment.environment` (`GRAFANA_APP_ENV` → `ASPNETCORE_ENVIRONMENT` → `Development`),
  `company` (`GRAFANA_COMPANY`), `product` (`GRAFANA_PRODUCT`), `service.type` (`GRAFANA_SERVICE_TYPE`),
  `server.region` (`SERVER_REGION`). <!-- verify:ignore -->
- Spans: ASP.NET Core (exceptions recorded) and outgoing `HttpClient`. Source `GiddhTemplate.*` is
  subscribed but no activity source is created in code. Unhandled exceptions add an `exception` event with
  `exception.type`, `exception.message`, `exception.stacktrace`, `http.method`, `http.route`. <!-- verify:ignore -->

### Logs (Serilog → console, files, Loki)
Sinks come from two places in `Program.cs:Main`: the `Serilog` section of `appsettings.json` (Console text,
File `/var/log/template-logs/giddh-template-.log` kept 7 days, `GrafanaLoki` at index 2) **and** code
(Console JSON, File JSON `/var/log/template-logs/giddh-template.log` kept 30 days).

- **Loki sink keys:** `Serilog:WriteTo:2:Args:uri`, `labels` (`app`, `environment`, `company`, `product`,
  `service_name`), `propertiesAsLabels` (`SourceContext`, `RequestId`), `httpHeaderName`/`httpHeaderValue`
  (`X-Scope-OrgID`). `GRAFANA_APP_ENV` overwrites the `environment` label
  (`Serilog:WriteTo:2:Args:labels:1:value`). <!-- verify:ignore -->
- **Properties on every event:** `Application`, `Version`, `Service`, `ServiceType`, `Environment`,
  `Company`, `Product`, `EnvironmentName`, `ProcessId`, `ThreadId`, `MachineName`. <!-- verify:ignore -->
- **Request log** (`UseSerilogRequestLogging`): `HTTP {RequestMethod} {RequestPath} → {StatusCode}
  ({Elapsed}ms) [{ContentLength}b] | TraceId: {TraceId}` plus `RequestHost`, `RequestScheme`,
  `QueryString`, `UserAgent`, `RemoteIpAddress`, `Referer`, `ContentType`, `SpanId`, `ParentId`,
  `RequestStartTime`. Level: Error for 5xx or exception, Warning for 4xx or > 5000 ms, else Information.
  <!-- verify:ignore -->
- **Unhandled exception log:** `Unhandled exception | Route | Method | TraceId | SpanId | UserAgent |
  RemoteIP | Query` at Error.
- Alloy also tails `/var/log/template-logs/*.log` and nginx logs into Loki, so application events can
  arrive in Loki more than once (inference from the sinks above plus `grafana-setup.sh`).
- Most render-pipeline diagnostics are `Console.WriteLine`, not Serilog: they reach stdout only.

> [!WARNING]
> `Services/GenericPdfService.cs` writes the first 2,000 characters of every generic payload to stdout
> (`SerializePayloadForLog`): customer names, addresses and tax numbers end up in instance logs.

### Slack alert contract
`Services/SlackService.cs:SendErrorAlertAsync` POSTs JSON to the URL in env var `SLACK_WEBHOOK_URL` (read
once from the environment, not from `IConfiguration`; unset → alert skipped with a console line):

```json
{ "url": "api/v1/pdf", "env": "<environment>", "error": "TimeoutException: … **Route:** … **TraceId:** …", "errorStackTrace": "…" }
```

- Shape is a flat object, not Slack's `{"text": …}`. The receiver is a relay that formats it for Slack
  (inference: the URL removed from `Services/SlackService.cs` in `a480da7` was not a Slack host).
- Sent by: the global exception handler (`env` = `ENVIRONMENT` → `GRAFANA_APP_ENV` →
  `ASPNETCORE_ENVIRONMENT` → `Development`), and each PDF controller on its own 500 paths (`env` =
  `ENVIRONMENT`, or the `Environment` configuration key in `AccountStatementController`).
- 10 s timeout; any failure is swallowed (console line). No retry, no rate limit: a burst of failures is a
  burst of alerts.

> [!WARNING]
> `SendErrorAlertAsync` prints the full webhook URL to stdout on every alert ("Sending error alert to
> Slack webhook URL: …"). Treat instance stdout as containing that secret.

### Swagger / OpenAPI
Not exposed. `Swashbuckle.AspNetCore` and `Microsoft.AspNetCore.OpenApi` are referenced in
`GiddhTemplate.csproj` and `Properties/launchSettings.json` opens `swagger`, but `Program.cs` registers no
Swagger generator or UI, so `/swagger` returns 404. This page is the API description.

## Required / Unavailable Backend Capabilities

When a change needs something this service does not do, specify the contract here and mark it pending.
Never fake it: no stub route that returns a canned PDF, no template that hard-codes data the payload lacks,
no silent default standing in for a missing field. The Giddh backend builds every payload; a new field
needs a matching change there, and this section is where that need is written down.

### Proposed Endpoint: none pending
- **Purpose:** No unmet capability is recorded as of `d462f69`.
- **Format for the next one:** method and path; which `templateType` it serves; required and optional
  JSON fields with types and which template reads them; response (`application/pdf` stream, filename);
  error statuses; and whether the Giddh backend change is merged, in progress or not started.
- **Blocking Status:** n/a
