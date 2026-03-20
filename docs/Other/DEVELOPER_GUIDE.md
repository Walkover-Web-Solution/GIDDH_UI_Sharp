# Giddh Template Service - Developer Guide

## Overview
**Giddh Template Service** is a .NET 8 web API that generates PDF invoices from dynamic templates using PuppeteerSharp and Razor templating. It supports multiple invoice templates with customizable styling and integrates with monitoring and alerting systems.

## Project Structure

```
GiddhTemplate/
├── Controllers/           # API Controllers → [Detailed Documentation](docs/CONTROLLERS.md)
├── Models/               # Data models and enums → [Detailed Documentation](docs/MODELS.md)
├── Services/             # Business logic services → [Detailed Documentation](docs/SERVICES.md)
├── Templates/            # Invoice templates and styling → [Detailed Documentation](docs/TEMPLATES.md)
├── Properties/           # Launch settings
├── Program.cs           # Application entry point → [Detailed Documentation](docs/PROGRAM.md)
├── appsettings.json     # Configuration
└── GiddhTemplate.csproj # Project file
```

## Configuration

### Core Settings (`@/Users/divyanshu/walkover/GiddhTemplate/appsettings.json`)

**Server Configuration:**
- **Port:** `5000` (HTTP)
- **Environment:** Configurable via `ASPNETCORE_ENVIRONMENT`

**Logging (Serilog):**
- **Console:** Debug level with JSON formatting
- **File:** `/var/log/template-logs/giddh-template-.log` (7-day retention)
- **Grafana Loki:** `http://loghub.msg91.com:3100` with structured labels

**Monitoring:**
- **OpenTelemetry:** Traces exported to `http://127.0.0.1:4318/v1/traces`
- **Prometheus:** Metrics endpoint available
- **Distributed Tracing:** Full request correlation

### Environment Variables
```bash
GRAFANA_APP_ENV=Production          # Environment label
APP_VERSION=1.0.0                   # Service version
SERVICE_NAME=giddh-template         # Service identifier
SLACK_WEBHOOK_URL=<webhook-url>     # Error alerting
GRAFANA_ORG_ID=<org-id>            # Telemetry organization
```

## Core Classes & Services

### 1. Controllers (`@/Users/divyanshu/walkover/GiddhTemplate/Controllers/PdfController.cs`)

**MainController:**
- `GET /` - Health check endpoint

**PdfController:**
- `POST /api/v1/pdf` - Generate PDF from invoice data
- **Input:** JSON payload matching `Root` model
- **Output:** PDF file (`application/pdf`)
- **Error Handling:** Slack integration for failures

### 2. Services

#### PdfService (`@/Users/divyanshu/walkover/GiddhTemplate/Services/PdfService.cs`)
**Purpose:** Core PDF generation using PuppeteerSharp

**Key Methods:**
- `GeneratePdfAsync(Root request)` - Main PDF generation
- `GetBrowserAsync()` - Singleton browser instance management
- `LoadStyles(string basePath)` - CSS loading for templates
- `LoadFontCSS(string fontFamily)` - Dynamic font loading

**Browser Configuration:**
```csharp
ExecutablePath = "/usr/bin/google-chrome"  // Production
Args = ["--no-sandbox", "--disable-setuid-sandbox", "--lang=en-US,ar-SA"]
```

#### RazorTemplateService (`@/Users/divyanshu/walkover/GiddhTemplate/Services/RazorTemplateService.cs`)
**Purpose:** Razor template compilation and rendering

**Features:**
- File system template loading
- Memory caching
- Model binding with strong typing

#### SlackService (`@/Users/divyanshu/walkover/GiddhTemplate/Services/SlackService.cs`)
**Purpose:** Error alerting and notifications

**Method:**
- `SendErrorAlertAsync(url, environment, error, stackTrace)`

### 3. Data Models (`@/Users/divyanshu/walkover/GiddhTemplate/Models/RequestModel.cs`)

**Root Model:** Main invoice data container with 400+ lines of structured data

**Key Components:**
- `Company` - Business information and branding
- `Settings` - Display preferences and field visibility
- `Theme` - Styling, fonts, colors, margins
- `CustomerDetails` - Client information
- `Entries` - Line items with taxes and discounts
- `Currency` - Multi-currency support
- `TaxBifurcation` - Tax breakdown and GST details

## Templates & Styling

### Template Types
1. **TemplateA** - Standard business invoice
2. **Tally** - Accounting software compatible format
3. **Thermal** - Receipt printer optimized

### Template Structure
Each template contains:
- `Header.cshtml` - Company info, logo, invoice details
- `Body.cshtml` - Line items, calculations, taxes
- `Footer.cshtml` - Terms, signatures, totals
- `Styles/` - Modular CSS files

### CSS Architecture (`@/Users/divyanshu/walkover/GiddhTemplate/Templates/TemplateA/Styles/Styles.css`)

**CSS Custom Properties:**
```css
--font-family: "Inter" | "Roboto" | "Open Sans" | "Lato"
--font-size-default: 14px
--font-size-medium: 12px  
--font-size-small: 10px
--color-primary: #181b50
--color-secondary: #6c757d
```

**Modular Styles:**
- `Styles.css` - Global variables and base styles
- `Header.css` - Header-specific styling
- `Body.css` - Content area styling  
- `Footer.css` - Footer styling
- `Background.css` - Page backgrounds (Tally only)

### Font Management
**Supported Fonts:** Inter, Roboto, Open Sans, Lato
**Location:** `@/Users/divyanshu/walkover/GiddhTemplate/Templates/Fonts/`
**Loading:** Dynamic CSS generation with `@font-face` declarations

## Dependencies (`@/Users/divyanshu/walkover/GiddhTemplate/GiddhTemplate.csproj`)

### Core Framework
- **.NET 8.0** - Target framework
- **ASP.NET Core** - Web API framework

### PDF Generation
- **PuppeteerSharp 20.1.1** - Headless Chrome automation
- **RazorLight 2.3.1** - Template engine

### Monitoring & Logging
- **Serilog 3.1.1** - Structured logging
- **OpenTelemetry 1.8.0** - Distributed tracing
- **Prometheus Exporter** - Metrics collection

### API Documentation
- **Swashbuckle.AspNetCore 6.6.2** - OpenAPI/Swagger

## Development Setup

### Prerequisites
- .NET 8 SDK
- Google Chrome (for PDF generation)
- Access to logging infrastructure

### Local Development
1. **Clone and restore:**
   ```bash
   git clone <repository>
   cd GiddhTemplate
   dotnet restore
   ```

2. **Configure Chrome path:**
   ```csharp
   // In PdfService.cs, uncomment local path:
   ExecutablePath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" // macOS
   // ExecutablePath = "C:/Program Files/Google/Chrome/Application/chrome.exe" // Windows
   ```

3. **Run application:**
   ```bash
   dotnet run
   # Service available at http://localhost:5000
   ```

### Testing PDF Generation

**TemplateA (Standard Business):**
```bash
curl -X POST http://localhost:5000/api/v1/pdf \
  -H "Content-Type: application/json" \
  -d @sample-payloads/TemplateA-Complete-Sample.json \
  --output templateA-invoice.pdf
```

**Tally (Accounting Format):**
```bash
curl -X POST http://localhost:5000/api/v1/pdf \
  -H "Content-Type: application/json" \
  -d @sample-payloads/Tally-Complete-Sample.json \
  --output tally-invoice.pdf
```

**Thermal (Receipt Printer):**
```bash
curl -X POST http://localhost:5000/api/v1/pdf \
  -H "Content-Type: application/json" \
  -d @sample-payloads/Thermal-Complete-Sample.json \
  --output thermal-receipt.pdf
```

## Error Handling & Monitoring

### Global Exception Handling
- **Centralized handler** in `@/Users/divyanshu/walkover/GiddhTemplate/Program.cs:151-216`
- **Rich context logging** with trace IDs, user agents, IP addresses
- **Automatic Slack alerting** for production errors
- **OpenTelemetry integration** with exception details

### Request Logging
- **Structured HTTP logging** with performance metrics
- **Slow request detection** (>5000ms flagged as warnings)
- **Distributed tracing** correlation across services

### Health Monitoring
- **Prometheus metrics** at `/metrics` endpoint
- **Custom metrics** for PDF generation performance
- **Browser instance monitoring** and automatic recovery

## Production Deployment

### Environment Configuration
```bash
ASPNETCORE_ENVIRONMENT=Production
GRAFANA_APP_ENV=Production
SERVICE_NAME=giddh-template
SLACK_WEBHOOK_URL=<production-webhook>
```

### File Permissions
- **Log directory:** `/var/log/template-logs/` (write access required)
- **Chrome binary:** `/usr/bin/google-chrome` (execute permissions)
- **Template files:** Read access to `Templates/` directory

### Performance Considerations
- **Browser reuse:** Singleton pattern prevents resource leaks
- **Memory caching:** Template compilation cached
- **Semaphore protection:** Thread-safe browser management
- **Font caching:** CSS generated once per font family

## API Reference

### POST /api/v1/pdf
**Purpose:** Generate PDF invoice from structured data

**Request:**
- **Content-Type:** `application/json`
- **Body:** `Root` model with invoice data

**Response:**
- **Success (200):** PDF file (`application/pdf`)
- **Error (400):** Invalid request format
- **Error (500):** PDF generation failure

**Sample Response Headers:**
```
Content-Type: application/pdf
Content-Disposition: attachment; filename="invoice.pdf"
```

## Troubleshooting

### Common Issues
1. **Chrome not found:** Verify `ExecutablePath` in `PdfService.cs`
2. **Template not rendering:** Check file paths and model binding
3. **Font not loading:** Verify font files in `Templates/Fonts/`
4. **Slack alerts failing:** Validate `SLACK_WEBHOOK_URL` environment variable

### Debug Logging
Enable detailed logging by setting:
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"
    }
  }
}
```

### CSS Print Media Queries
```css
@media print {
    .no-print { display: none; }
    .page-break { page-break-before: always; }
    body { -webkit-print-color-adjust: exact; }
}
```

### Repeating Headers and Footers
For multi-page invoices with repeating headers/footers on each page, refer to this comprehensive guide:
[The Ultimate Print HTML Template with Header Footer](https://medium.com/@Idan_Co/the-ultimate-print-html-template-with-header-footer-568f415f6d2a)

---

**Last Updated:** December 2025  
**Version:** 1.0.0  
**Author/Developer:** Divyanshu Shrivastava  
**Maintainer:** Walkover Web Solutions
