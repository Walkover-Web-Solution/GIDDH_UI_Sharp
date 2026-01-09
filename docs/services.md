# Services Documentation

## Overview
The Services directory contains the core business logic for PDF generation, template rendering, and external integrations for the Giddh Template Service.

## Service Classes

### 1. PdfService (`@/Users/divyanshu/walkover/GiddhTemplate/Services/PdfService.cs`)

**Purpose:** Core PDF generation using PuppeteerSharp and headless Chrome

#### Constructor and Dependencies
```csharp
public PdfService()
{
    _razorTemplateService = new RazorTemplateService();
}
```

**Private Fields:**
- `RazorTemplateService _razorTemplateService` - Template rendering engine
- `string _openSansFontCSS` - Cached Open Sans font CSS
- `string _robotoFontCSS` - Cached Roboto font CSS  
- `string _latoFontCSS` - Cached Lato font CSS
- `string _interFontCSS` - Cached Inter font CSS
- `SemaphoreSlim _semaphore` - Thread-safe browser management
- `IBrowser? _browser` - Singleton browser instance

#### Core Methods

##### GeneratePdfAsync(Root request)
**Purpose:** Main PDF generation workflow

**Process Flow:**
1. **Template Selection:** Based on `request.TemplateType`
2. **Style Loading:** CSS files for selected template
3. **Font Processing:** Dynamic font CSS generation
4. **HTML Rendering:** Razor template compilation
5. **PDF Generation:** PuppeteerSharp conversion
6. **Resource Cleanup:** Browser page disposal

**Template Types:**
- `"TemplateA"` - Standard business invoice
- `"Tally"` - Accounting software format
- `"Thermal"` - Receipt printer format

##### GetBrowserAsync()
**Purpose:** Singleton browser instance management

**Implementation:**
```csharp
public async Task<IBrowser> GetBrowserAsync()
{
    if (_browser == null || !_browser.IsConnected)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_browser == null || !_browser.IsConnected)
            {
                var launchOptions = new LaunchOptions
                {
                    Headless = true,
                    ExecutablePath = "/usr/bin/google-chrome", // Production
                    Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--lang=en-US,ar-SA" }
                };
                _browser = await Puppeteer.LaunchAsync(launchOptions);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
    return _browser!;
}
```

**Browser Configuration:**
- **Headless Mode:** No GUI required
- **Sandbox Disabled:** For containerized environments
- **Multi-language Support:** English and Arabic
- **Thread Safety:** Semaphore protection

##### LoadStyles(string basePath)
**Purpose:** Load modular CSS files for templates

**Return Type:** Tuple with named components
```csharp
public (string Common, string Header, string Footer, string Body, string Background)
    LoadStyles(string basePath)
```

**CSS Files Loaded:**
- `Styles.css` - Global variables and base styles
- `Header.css` - Header-specific styling
- `Footer.css` - Footer-specific styling
- `Body.css` - Content area styling
- `Background.css` - Page backgrounds (Tally only)

##### LoadFontCSS(string fontFamily)
**Purpose:** Dynamic font CSS generation with caching

**Supported Fonts:**
- **Open Sans** - Modern sans-serif
- **Roboto** - Google's material design font
- **Lato** - Humanist sans-serif
- **Inter** - UI-optimized font (default)

**Font Loading Process:**
```csharp
public string LoadFontCSS(string fontFamily)
{
    if (fontFamily == "Open Sans" && string.IsNullOrEmpty(_openSansFontCSS))
    {
        string fontPath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "Fonts", "OpenSans");
        _openSansFontCSS = BuildFontCSS("Open Sans", fontPath);
    }
    // Similar for other fonts...
}
```

##### BuildFontCSS(string fontFamily, string fontPath)
**Purpose:** Generate @font-face CSS declarations

**Font Weights Supported:**
- 200 (Light)
- 400 (Regular)
- 500 (Medium)
- 700 (Bold)

**CSS Generation:**
```css
@font-face {
    font-family: 'FontName';
    src: url('data:font/woff2;base64,{base64data}') format('woff2');
    font-weight: 400;
    font-style: normal;
}
```

#### Error Handling

**Browser Launch Failures:**
```csharp
catch (PuppeteerSharp.ProcessException)
{
    _browser = null;
    throw;
}
```

**Resource Management:**
- Automatic browser recovery on disconnection
- Page disposal after PDF generation
- Memory-efficient font caching

### 2. RazorTemplateService (`@/Users/divyanshu/walkover/GiddhTemplate/Services/RazorTemplateService.cs`)

**Purpose:** Razor template compilation and rendering

#### Constructor Configuration
```csharp
public RazorTemplateService()
{
    _engine = new RazorLightEngineBuilder()
        .UseEmbeddedResourcesProject(typeof(RazorTemplateService)) 
        .UseFileSystemProject(Directory.GetCurrentDirectory())   
        .UseMemoryCachingProvider()
        .Build();
}
```

**Configuration Features:**
- **Embedded Resources:** Support for embedded templates
- **File System:** Template loading from disk
- **Memory Caching:** Compiled template caching
- **Performance Optimization:** Reduced compilation overhead

#### Core Method

##### RenderTemplateAsync<T>(string templatePath, T model)
**Purpose:** Compile and render Razor templates with strongly-typed models

**Process:**
1. **File Validation:** Check template existence
2. **Content Loading:** Read template file
3. **Compilation:** Razor syntax processing
4. **Rendering:** Model binding and HTML generation

**Implementation:**
```csharp
public async Task<string> RenderTemplateAsync<T>(string templatePath, T model)
{
    if (!File.Exists(templatePath))
    {
        throw new FileNotFoundException($"Template file not found: {templatePath}");
    }

    string templateContent = await File.ReadAllTextAsync(templatePath);

    return await _engine.CompileRenderStringAsync(
        templatePath,
        templateContent,
        model
    );
}
```

**Error Handling:**
- File existence validation
- Template compilation error propagation
- Model binding validation

### 3. SlackService (`@/Users/divyanshu/walkover/GiddhTemplate/Services/SlackService.cs`)

**Purpose:** Error notification and alerting integration

#### Constructor and Configuration
```csharp
public SlackService(IConfiguration configuration)
{
    _slackWebhookUrl = Environment.GetEnvironmentVariable("SLACK_WEBHOOK_URL");
}
```

**Environment Variables:**
- `SLACK_WEBHOOK_URL` - Webhook endpoint for notifications

#### Core Method

##### SendErrorAlertAsync(string url, string environment, string error, string stackTrace)
**Purpose:** Send structured error alerts to Slack

**Payload Structure:**
```csharp
var keyValuePairs = new Dictionary<string, string>
{
    { "url", url },
    { "env", environment },
    { "error", error },
    { "errorStackTrace", stackTrace }
};
```

**HTTP Configuration:**
```csharp
using var _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
var json = JsonSerializer.Serialize(keyValuePairs);
var content = new StringContent(json, Encoding.UTF8, "application/json");
```

**Error Handling:**
```csharp
try
{
    var response = await _httpClient.PostAsync(_slackWebhookUrl, content);
    if (!response.IsSuccessStatusCode)
    {
        var errorContent = await response.Content.ReadAsStringAsync();
        throw new Exception($"Failed to send error alert: {response.StatusCode} - {errorContent}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error while sending alert on slack: {ex.Message}");
    // Don't throw to avoid breaking main flow
}
```

**Resilience Features:**
- 10-second timeout for webhook calls
- Non-blocking error handling
- Graceful degradation on Slack failures

### 4. ISlackService Interface (`@/Users/divyanshu/walkover/GiddhTemplate/Services/ISlackService.cs`)

**Purpose:** Abstraction for Slack service implementation

```csharp
public interface ISlackService
{
    Task SendErrorAlertAsync(string url, string environment, string error, string stackTrace);
}
```

**Benefits:**
- Dependency injection support
- Unit testing with mocks
- Implementation flexibility
- Service decoupling

## Service Integration Patterns

### Dependency Injection Registration (`@/Users/divyanshu/walkover/GiddhTemplate/Program.cs:140-143`)
```csharp
builder.Services.AddScoped<ISlackService, SlackService>();
builder.Services.AddScoped<PdfService>();
```

**Service Lifetimes:**
- **Scoped:** Per-request instances
- **Thread Safety:** Semaphore protection for shared resources
- **Resource Management:** Proper disposal patterns

### Service Composition
```csharp
// PdfController constructor
public PdfController(PdfService pdfService, ISlackService slackService, IConfiguration configuration)
```

**Service Dependencies:**
- `PdfService` → `RazorTemplateService` (composition)
- `PdfController` → `PdfService` + `ISlackService` (injection)
- `SlackService` → `IConfiguration` (injection)

## Performance Considerations

### Browser Management
- **Singleton Pattern:** Single browser instance across requests
- **Connection Monitoring:** Automatic reconnection on failures
- **Resource Pooling:** Efficient page creation and disposal

### Template Caching
- **Compiled Templates:** Memory-cached Razor compilation
- **Font CSS Caching:** One-time font processing per family
- **Style Loading:** Efficient file system access

### Memory Management
- **Async Operations:** Non-blocking I/O operations
- **Resource Disposal:** Proper cleanup of browser pages
- **Garbage Collection:** Minimal object allocation

## Error Handling Strategy

### Service-Level Errors
- **Browser Failures:** Automatic recovery and retry
- **Template Errors:** Detailed error propagation
- **Network Failures:** Graceful degradation

### Monitoring Integration
- **Slack Alerts:** Production error notifications
- **Structured Logging:** Detailed error context
- **Distributed Tracing:** Request correlation

## Testing Considerations

### Unit Testing
- **Mock Dependencies:** ISlackService interface mocking
- **Template Testing:** Isolated Razor rendering tests
- **Browser Mocking:** PuppeteerSharp test doubles

### Integration Testing
- **End-to-End PDF Generation:** Full workflow testing
- **Template Rendering:** Real template compilation
- **Error Scenarios:** Failure mode validation

---

**Author/Developer:** Divyanshu Shrivastava  
**Last Updated:** December 2025
