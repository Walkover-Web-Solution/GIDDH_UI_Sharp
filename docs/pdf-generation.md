# PDF Generation Guide

This guide provides step-by-step instructions for generating PDFs using the GiddhTemplate project.

## Prerequisites

- .NET SDK installed
- Google Chrome browser installed
- Access to Giddh application (books.giddh.com or test.giddh.com)
- Postman or similar API testing tool

## Step-by-Step Process

### Step 1: Access Giddh Application
Go to either:
- `books.giddh.com` (production)
- `test.giddh.com` (test environment)

### Step 2: Preview Invoice PDF
Navigate to any invoice in Giddh and preview the PDF from anywhere in the application.

### Step 3: Capture API Call
1. Open browser developer tools (F12)
2. Go to Network tab
3. Preview the invoice PDF
4. Look for the `/download-file` API call
5. Right-click on the API call and copy as cURL

### Step 4: Add Debug Mode
Modify the copied cURL by adding the query parameter:
```
debugMode=true
```

### Step 5: Execute API Call and Copy Response
1. Execute the modified cURL command
2. Copy the entire response body (JSON object)
3. Save this payload - you'll use it for testing

### Step 6: Configure Chrome Path in PdfService.cs
Navigate to `/Services/PdfService.cs` file and locate lines 41-43:

```csharp
ExecutablePath = "/usr/bin/google-chrome", // Server Google Chrome path
// ExecutablePath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", // Local path MacOS
// ExecutablePath = "C:/Program Files/Google/Chrome/Application/chrome.exe", // Local path Windows
```

**For Local Development:**
1. Comment out the server path (line 41)
2. Uncomment the appropriate local path:
   - **MacOS**: Uncomment line 42
   - **Windows**: Uncomment line 43

**Example for MacOS:**
```csharp
// ExecutablePath = "/usr/bin/google-chrome", // Server Google Chrome path
ExecutablePath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", // Local path MacOS
// ExecutablePath = "C:/Program Files/Google/Chrome/Application/chrome.exe", // Local path Windows
```

### Step 7: Run the Project
Execute the following command in the project root directory:
```bash
dotnet run
```

The application will start and listen on `http://localhost:5000`

### Step 8: Test PDF Generation with Custom Naming
1. Open Postman or your preferred API testing tool
2. Create a POST request to: `http://localhost:5000/api/v1/pdf`
3. Set the request body to the JSON payload you copied from Step 5
4. **Optional**: Add custom filename by including `pdfRename` in your payload:
   ```json
   {
     "pdfRename": "MyCustomInvoice_Dec2025",
     "templateType": "TemplateA",
     // ... rest of your payload
   }
   ```
5. Send the request

### Step 9: Verify Results
After sending the request, you should:
1. See the PDF preview in Postman response
2. **In Development Environment**: Find the generated PDF file in the local `/Downloads` folder with your custom name (if specified)
3. **File Location Examples**:
   - Without pdfRename: `/Downloads/PDF_20251223134500.pdf`
   - With pdfRename: `/Downloads/MyCustomInvoice_Dec2025.pdf`
   - Duplicate handling: `/Downloads/MyCustomInvoice_Dec2025_1.pdf`

### Step 10: Generate Additional PDFs
To generate new PDFs:
1. Simply hit the POST API endpoint from Postman with different payloads
2. The service will generate PDFs based on the provided data

## Important Notes

### Code Changes and Restart
- **Important**: If you make any changes to the code, you must restart the application using `dotnet run` to see the impact in PDF generation.

### Chrome Path Configuration
Make sure to use the correct Chrome executable path for your operating system:

- **Linux/Server**: `/usr/bin/google-chrome`
- **MacOS**: `/Applications/Google Chrome.app/Contents/MacOS/Google Chrome`
- **Windows**: `C:/Program Files/Google/Chrome/Application/chrome.exe`

### API Endpoint
- **URL**: `http://localhost:5000/api/v1/pdf`
- **Method**: POST
- **Content-Type**: application/json
- **Body**: JSON payload from Giddh's `/download-file` API response

### Supported Template Types
The service supports multiple template types:
- **TemplateA** (default)
- **Tally**
- **Thermal**

### Output Location and File Naming

#### Automatic PDF Saving (Development Environment Only)
Generated PDF files are automatically saved to the `/Downloads` directory in the project root **only in Development, Local, or unspecified environments**. This feature is disabled in production environments for security and performance reasons.

#### PDF File Naming with pdfRename Feature (Local/Development Only)
The service supports custom PDF file naming through the `pdfRename` parameter in your request payload. **Note: This feature only works in local/development environments and is disabled in production.**

**Default Naming (without pdfRename):**
```
PDF_20251223134500.pdf  // Format: PDF_YYYYMMDDHHMMSS.pdf
```

**Custom Naming (with pdfRename):**
```json
{
  "pdfRename": "Invoice_ABC_Company_Dec2025",
  // ... other payload data
}
```
Results in: `Invoice_ABC_Company_Dec2025.pdf`

#### File Naming Rules and Safety Features
- **Automatic Sanitization**: Invalid filename characters are automatically removed or replaced
- **Duplicate Prevention**: If a file with the same name exists, a counter is appended (e.g., `filename_1.pdf`, `filename_2.pdf`)
- **Character Limits**: Long filenames are truncated to ensure filesystem compatibility
- **Cross-Platform Support**: Generated filenames work across Windows, macOS, and Linux systems

#### Environment-Based Behavior
```csharp
// Both pdfRename feature and automatic saving work only in these environments:
- ASPNETCORE_ENVIRONMENT = "Development"
- ENVIRONMENT = "Local" 
- No environment variable set (defaults to development behavior)

// Production environments skip both custom naming and automatic file saving
- ASPNETCORE_ENVIRONMENT = "Production"
- ENVIRONMENT = "Production"
```

## Troubleshooting

### Common Issues

1. **Chrome not found error**
   - Verify Chrome is installed
   - Check the ExecutablePath is correct for your OS
   - Ensure Chrome is accessible from the specified path

2. **Port already in use**
   - Check if another instance is running
   - Kill existing processes or use a different port

3. **PDF generation fails**
   - Verify the JSON payload is valid
   - Check Chrome browser permissions
   - Ensure all required fonts are available

4. **Template not found**
   - Verify template files exist in `/Templates` directory
   - Check template type in the request payload

### Debug Mode Benefits
Using `debugMode=true` in the original API call provides:
- Complete request payload structure
- All required fields for PDF generation
- Proper data formatting examples

## Development Workflow

1. Make code changes in the project
2. Stop the running application (Ctrl+C)
3. Run `dotnet run` to restart with changes
4. Test PDF generation via Postman
5. Repeat as needed

This workflow ensures that all code changes are reflected in the PDF generation process.
