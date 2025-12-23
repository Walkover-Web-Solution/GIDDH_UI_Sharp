# Controllers Documentation

## Overview
The Controllers directory contains the API controllers that handle HTTP requests and responses for the Giddh Template Service.

## Controller Classes

### 1. MainController (`@/Users/divyanshu/walkover/GiddhTemplate/Controllers/PdfController.cs:10-17`)

**Purpose:** Provides basic health check functionality

**Endpoints:**
- `GET /` - Returns service status

**Implementation:**
```csharp
[ApiController]
public class MainController : ControllerBase
{
    [HttpGet("/")]
    public IActionResult Get()
    {
        return Ok("Hello from Giddh template!");
    }
}
```

**Usage:**
- Health monitoring
- Service availability verification
- Load balancer health checks

### 2. PdfController (`@/Users/divyanshu/walkover/GiddhTemplate/Controllers/PdfController.cs:19-66`)

**Purpose:** Main API controller for PDF generation functionality

#### Constructor Dependencies
```csharp
public PdfController(PdfService pdfService, ISlackService slackService, IConfiguration configuration)
```

**Dependencies:**
- `PdfService` - Core PDF generation logic
- `ISlackService` - Error notification service
- `IConfiguration` - Environment configuration access

#### Endpoints

##### POST /api/v1/pdf
**Purpose:** Generate PDF invoice from JSON data

**Request:**
- **Method:** POST
- **Content-Type:** `application/json`
- **Body:** JSON object matching `Root` model structure

**Response:**
- **Success (200):** PDF file with headers:
  - `Content-Type: application/pdf`
  - `Content-Disposition: attachment; filename="invoice.pdf"`
- **Error (400):** Invalid request format
- **Error (500):** PDF generation failure

**Implementation Flow:**
1. **Request Deserialization:**
   ```csharp
   var jsonString = JsonSerializer.Serialize(requestObj);
   Root request = JsonSerializer.Deserialize<Root>(jsonString,
       new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
   ```

2. **Validation:**
   ```csharp
   if (request == null || string.IsNullOrEmpty(request.Company?.Name))
   {
       return BadRequest("Invalid request data. Ensure payload matches expected format.");
   }
   ```

3. **PDF Generation:**
   ```csharp
   byte[] pdfBytes = await _pdfService.GeneratePdfAsync(request);
   ```

4. **Error Handling:**
   ```csharp
   if (pdfBytes == null || pdfBytes.Length == 0)
   {
       await _slackService.SendErrorAlertAsync(
           url: "api/v1/pdf",
           environment: _environment,
           error: "PDF generation returned empty result.",
           stackTrace: "No stacktrace (service returned empty bytes)."
       );
       return StatusCode(500, new { error = "Failed to generate PDF!" });
   }
   ```

5. **Response:**
   ```csharp
   return File(pdfBytes, "application/pdf", "invoice.pdf");
   ```

## Error Handling Strategy

### Validation Errors (400)
- Missing required fields in request
- Invalid JSON structure
- Null or empty company name

### Server Errors (500)
- PDF generation failures
- Template rendering issues
- Browser automation problems
- Automatic Slack notification for production issues

## Environment Integration

**Environment Detection:**
```csharp
_environment = Environment.GetEnvironmentVariable("ENVIRONMENT");
```

**Usage:**
- Error alert context
- Environment-specific behavior
- Logging correlation

## API Design Patterns

### RESTful Design
- Resource-based URLs (`/api/v1/pdf`)
- HTTP status codes for response indication
- JSON content negotiation

### Dependency Injection
- Constructor injection for services
- Interface-based abstractions
- Scoped service lifetimes

### Async/Await Pattern
- Non-blocking PDF generation
- Scalable request handling
- Proper resource disposal

## Testing Considerations

### Unit Testing
- Mock `PdfService` for controller logic testing
- Mock `ISlackService` for error handling verification
- Test request validation scenarios

### Integration Testing
- End-to-end PDF generation flow
- Error handling with real services
- Performance testing with large payloads

### Sample Test Request
```json
{
  "company": {
    "name": "Test Company",
    "address": "123 Test Street"
  },
  "voucherNumber": "INV-001",
  "voucherDate": "2025-12-23",
  "entries": [
    {
      "accountName": "Test Item",
      "amount": {
        "amountForAccount": 100.00
      }
    }
  ]
}
```

## Security Considerations

### Input Validation
- JSON deserialization with case-insensitive options
- Required field validation
- Payload size limitations (implicit via ASP.NET Core)

### Error Information Disclosure
- Generic error messages for client responses
- Detailed logging for internal debugging
- Sensitive information excluded from responses

## Performance Optimization

### Async Operations
- Non-blocking PDF generation
- Concurrent request handling
- Resource-efficient processing

### Memory Management
- Byte array handling for PDF data
- Proper disposal of resources
- Streaming responses for large files

---

**Author/Developer:** Divyanshu Shrivastava  
**Last Updated:** December 2025
