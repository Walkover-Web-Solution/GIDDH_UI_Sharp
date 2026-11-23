# Open Questions

Questions below are not answered in repository sources (README, docs, code, or commit messages). An owner should decide and reflect answers in `docs/VISION.md` when resolved.

1. **Service exposure** — Is this API intended only for internal Giddh backend callers, or may third parties integrate directly? `Properties/launchSettings.json` enables anonymous authentication; no in-service JWT/API-key middleware appears in `Program.cs`.

2. **Canonical PDF API surface** — Should new work extend `POST /api/v1/pdf` (`Root` model), `POST /api/v1/genericpdf` (dynamic `templateType` map), or both long term? Documentation emphasizes `/api/v1/pdf` while `GenericPdfService` hosts many additional templates under `Templates/other-template/`.

3. **Success metrics and SLOs** — No stated targets for latency, availability, concurrent throughput, or error rate. MEMORY and ops docs describe constraints but not customer-facing SLOs.

4. **Certificate PDFs** — Recent commits add `CertificateController` and certificate templates. Not stated: whether certificates are a supported product surface for Giddh customers or an experimental/internal endpoint.

5. **Template governance** — Who approves new `templateType` values and Razor templates (naming, GST compliance, branding)? Process is not documented.

6. **Diagnostics endpoints** — `api/diagnostics/memory`, `api/diagnostics/gc`, and related routes are operational tooling. Not stated: whether they must remain unauthenticated, be network-restricted only, or receive explicit auth.

7. **Deprecation of in-memory PDF responses** — Disk streaming is implemented for main controllers; not stated whether returning large `byte[]` responses anywhere is permanently out of scope.

8. **Relationship to `api.giddh.com`** — Docs describe capturing payloads from Giddh’s `download-file` API with Bearer JWT. Not stated: the formal contract version between Giddh API teams and this service (schema ownership, versioning).