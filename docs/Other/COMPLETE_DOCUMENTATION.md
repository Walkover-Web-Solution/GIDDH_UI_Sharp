# GiddhTemplate - Complete Documentation

**PDF Generation Service for Giddh Application**

A comprehensive .NET 8.0 web application designed for generating high-quality PDF documents from HTML templates using PuppeteerSharp and Razor templating engine. This service provides enterprise-grade PDF generation capabilities with support for multiple template types, dynamic theming, and extensive customization options.

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [First Time Setup Guide](#first-time-setup-guide)
3. [PDF Generation Guide](#pdf-generation-guide)
4. [Complete API Documentation](#complete-api-documentation)
5. [Payload Structure and Examples](#payload-structure-and-examples)
6. [Template System Architecture](#template-system-architecture)
7. [Font Management System](#font-management-system)
8. [Theming and Customization](#theming-and-customization)
9. [Error Handling and Response Formats](#error-handling-and-response-formats)
10. [Comprehensive Troubleshooting](#comprehensive-troubleshooting)
11. [Development Guidelines](#development-guidelines)
12. [Performance Optimization](#performance-optimization)
13. [Security Considerations](#security-considerations)

---

## Project Overview

### Comprehensive Feature Set

#### Core PDF Generation Features
- **Advanced PDF Generation**: Convert complex HTML templates to high-quality PDF documents using Chrome headless browser technology
- **Multi-Template Support**: Comprehensive support for three distinct template types:
  - **TemplateA**: Standard business invoice and document templates with full customization
  - **Tally**: Specialized templates for Tally ERP integration with accounting-specific formatting
  - **Thermal**: Optimized templates for thermal printer output with compact layouts
- **Dynamic Font Management**: Intelligent font loading system supporting multiple font families with automatic CSS generation
- **Advanced Razor Templating**: Server-side HTML generation using Razor syntax with conditional rendering and data binding
- **RESTful API Architecture**: Clean, well-documented HTTP API endpoints for seamless integration

#### Advanced Capabilities
- **Theme System**: Comprehensive theming support with color schemes, font customization, and margin controls
- **Multi-Currency Support**: Handle multiple currencies with proper formatting and conversion rates
- **Tax Calculation Engine**: Built-in support for complex tax calculations including GST, TDS, TCS, and other tax types
- **QR Code Generation**: Automatic QR code generation for invoices and payment integration
- **Digital Signatures**: Support for digital signature integration and image-based signatures
- **Watermarking**: Configurable watermark and seal application
- **Internationalization**: Multi-language support with proper text rendering
- **Custom PDF Naming**: Advanced `pdfRename` feature for custom file naming with automatic sanitization (local/development environments only)
- **Environment-Aware File Saving**: Intelligent PDF saving behavior based on deployment environment

### Technology Stack and Dependencies

#### Core Framework
- **.NET 8.0** - Latest long-term support version of .NET with enhanced performance and features
- **ASP.NET Core 8.0** - Modern web framework for building high-performance web APIs
- **C# 12** - Latest C# language features for improved developer productivity

#### PDF Generation Engine
- **PuppeteerSharp v20.1.1** - .NET wrapper for Puppeteer providing Chrome automation capabilities
- **Google Chrome Headless** - Rendering engine for high-fidelity PDF generation
- **HTML5/CSS3** - Modern web standards for template rendering

#### Template Processing
- **RazorLight v2.3.1** - Lightweight Razor template engine for server-side HTML generation
- **System.Text.Json** - High-performance JSON serialization and deserialization

#### Logging and Monitoring
- **Serilog v3.1.1** - Structured logging framework with rich formatting capabilities
- **Serilog.AspNetCore v8.0.0** - ASP.NET Core integration for Serilog

#### API Documentation
- **Swashbuckle.AspNetCore v6.6.2** - OpenAPI/Swagger documentation generation
- **Microsoft.AspNetCore.OpenApi v8.0.11** - OpenAPI specification support

#### Dependency Injection
- **Microsoft.Extensions.DependencyInjection.Abstractions v8.0.0** - Dependency injection container

### Detailed Project Structure

```
GiddhTemplate/
├── Controllers/                    # API Controllers
│   └── PdfController.cs           # Main PDF generation endpoint
├── Models/                         # Data Models and DTOs
│   ├── Enums/                     # Enumeration definitions
│   │   └── Common.enum.cs         # Common enumerations
│   └── RequestModel.cs            # Request/Response models
├── Services/                       # Business Logic Services
│   ├── PdfService.cs              # Core PDF generation service
│   ├── RazorTemplateService.cs    # Template rendering service
│   ├── SlackService.cs            # Notification service
│   └── ISlackService.cs           # Service interface
├── Templates/                      # HTML Templates and Assets
│   ├── Fonts/                     # Font Files
│   │   ├── Inter/                 # Inter font family
│   │   ├── Lato/                  # Lato font family
│   │   ├── OpenSans/              # Open Sans font family
│   │   └── Roboto/                # Roboto font family
│   ├── Tally/                     # Tally Template Files
│   │   ├── Styles/                # CSS stylesheets
│   │   ├── Body.cshtml            # Main content template
│   │   ├── Footer.cshtml          # Footer template
│   │   └── Header.cshtml          # Header template
│   ├── TemplateA/                 # Default Template Files
│   │   ├── Styles/                # CSS stylesheets
│   │   ├── Body.cshtml            # Main content template
│   │   ├── Footer.cshtml          # Footer template
│   │   ├── Header.cshtml          # Header template
│   │   ├── PO_PB_Body.cshtml      # Purchase Order/Bill body
│   │   ├── PO_PB_Header.cshtml    # Purchase Order/Bill header
│   │   └── Receipt_Payment_Body.cshtml # Receipt/Payment body
│   └── Thermal/                   # Thermal Template Files
│       ├── Styles/                # CSS stylesheets
│       └── Body.cshtml            # Thermal printer template
├── docs/                          # Documentation Files
│   ├── setup.md                   # Setup instructions
│   ├── pdf-generation.md          # PDF generation guide
│   ├── controllers.md             # Controller documentation
│   ├── services.md                # Service documentation
│   ├── models.md                  # Model documentation
│   ├── templates.md               # Template documentation
│   └── program.md                 # Configuration documentation
├── sample-payloads/               # Sample JSON Payloads
│   ├── TemplateA-Complete-Sample.json
│   ├── Tally-Complete-Sample.json
│   └── Thermal-Complete-Sample.json
├── Downloads/                     # Generated PDF Output
├── .ebextensions/                 # AWS Elastic Beanstalk configuration
├── .platform/                     # Platform-specific configurations
├── Properties/                    # Application properties
│   └── launchSettings.json       # Launch configuration
├── GiddhTemplate.csproj          # Project file
├── Program.cs                    # Application entry point
├── appsettings.json              # Application configuration
├── appsettings.Development.json  # Development configuration
├── README.md                     # Project overview
└── COMPLETE_DOCUMENTATION.md    # This comprehensive documentation
```

---

## First Time Setup Guide

### Comprehensive Prerequisites and System Requirements

#### Essential Software Requirements
Before beginning the installation process, ensure your system meets the following requirements:

**Required Software:**
- **Git Version Control System**: Latest version (2.40+) for repository cloning and version management
- **Stable Internet Connection**: Required for downloading dependencies, NuGet packages, and Chrome browser
- **Text Editor or IDE**: Visual Studio 2022, Visual Studio Code, or JetBrains Rider for development

#### Operating System Requirements

**Windows Systems:**
- **Operating System**: Windows 10 version 1909 or later, Windows 11, or Windows Server 2019/2022
- **Architecture**: x64 (64-bit) processor architecture required
- **Memory**: Minimum 4GB RAM, recommended 8GB or higher for optimal performance
- **Storage**: At least 2GB free disk space for installation and generated files
- **Chrome Browser**: Google Chrome version 90+ installed at standard location

**macOS Systems:**
- **Operating System**: macOS 10.15 (Catalina) or later, including macOS Monterey, Ventura, and Sonoma
- **Architecture**: Intel x64 or Apple Silicon (M1/M2) processors supported
- **Memory**: Minimum 4GB RAM, recommended 8GB or higher
- **Storage**: At least 2GB free disk space
- **Chrome Browser**: Google Chrome installed in Applications folder

**Linux Systems:**
- **Distributions**: Ubuntu 18.04+, Debian 10+, CentOS 7+, RHEL 7+, SUSE 12+, Alpine 3.14+
- **Architecture**: x64 processor architecture
- **Memory**: Minimum 4GB RAM, recommended 8GB or higher
- **Storage**: At least 2GB free disk space
- **Chrome Browser**: Google Chrome or Chromium browser installed

#### Network and Security Requirements
- **Firewall Configuration**: Ensure ports 5000 and 5001 are available for local development
- **Antivirus Exclusions**: Add project directory to antivirus exclusions to prevent interference
- **Proxy Settings**: Configure proxy settings if behind corporate firewall

### Detailed Setup Steps

#### Step 1: Repository Cloning and Initial Setup

**Clone the Repository:**
```bash
# Clone the main repository
git clone https://github.com/Walkover-Web-Solution/GIDDH_UI_Sharp

# Navigate to the project directory
cd GIDDH_UI_Sharp

# Verify the repository structure
ls -la
```

**Expected Repository Contents:**
After cloning, you should see the following directory structure:
```
GIDDH_UI_Sharp/
├── Controllers/
├── Models/
├── Services/
├── Templates/
├── docs/
├── sample-payloads/
├── GiddhTemplate.csproj
├── Program.cs
├── README.md
└── appsettings.json
```

#### Step 2: .NET 8.0 SDK Installation and Configuration

**Download and Install .NET 8.0 SDK:**

1. **Visit the Official Download Page:**
   - Navigate to: https://dotnet.microsoft.com/download/dotnet/8.0
   - Select the appropriate installer for your operating system

2. **Windows Installation:**
   ```bash
   # Download the Windows x64 installer
   # Run the installer with administrator privileges
   # Follow the installation wizard prompts
   ```

3. **macOS Installation:**
   ```bash
   # Download the macOS installer (.pkg file)
   # Double-click the installer and follow prompts
   # Or use Homebrew:
   brew install --cask dotnet
   ```

4. **Linux Installation (Ubuntu/Debian):**
   ```bash
   # Add Microsoft package repository
   wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
   sudo dpkg -i packages-microsoft-prod.deb
   
   # Update package index
   sudo apt-get update
   
   # Install .NET 8.0 SDK
   sudo apt-get install -y dotnet-sdk-8.0
   ```

5. **Linux Installation (CentOS/RHEL):**
   ```bash
   # Add Microsoft package repository
   sudo rpm -Uvh https://packages.microsoft.com/config/centos/7/packages-microsoft-prod.rpm
   
   # Install .NET 8.0 SDK
   sudo yum install dotnet-sdk-8.0
   ```

**Verify Installation:**
```bash
# Check .NET version
dotnet --version
# Expected output: 8.0.x

# List installed SDKs
dotnet --list-sdks
# Should show .NET 8.0.x SDK

# List installed runtimes
dotnet --list-runtimes
# Should show ASP.NET Core 8.0.x runtime
```

**Configure Development Environment:**
```bash
# Set environment variables (optional)
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# Configure NuGet sources (if needed)
dotnet nuget list source
```

#### Step 3: Project Navigation and Structure Verification

**Navigate to Project Directory:**
```bash
# Ensure you're in the correct directory
cd GIDDH_UI_Sharp

# Verify project file exists
ls -la GiddhTemplate.csproj

# Check project structure
tree -L 2  # On Linux/macOS
# or
dir /s    # On Windows
```

**Verify Project Configuration:**
```bash
# Display project information
dotnet list package

# Check target framework
dotnet list package --framework net8.0

# Verify project builds without errors
dotnet build --configuration Debug --verbosity normal
```

#### Step 4: Comprehensive Dependency Restoration

**Clean Previous Builds (if any):**
```bash
# Remove previous build artifacts
dotnet clean

# Clear NuGet cache (if needed)
dotnet nuget locals all --clear
```

**Restore All Dependencies:**
```bash
# Restore NuGet packages with detailed output
dotnet restore --verbosity detailed

# Verify specific packages are restored
dotnet list package --include-transitive
```

**Expected Package Restoration:**
The following packages should be successfully restored:

```xml
<!-- Core Framework Packages -->
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.11" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />

<!-- PDF Generation Packages -->
<PackageReference Include="PuppeteerSharp" Version="20.1.1" />

<!-- Template Engine Packages -->
<PackageReference Include="RazorLight" Version="2.3.1" />

<!-- API Documentation Packages -->
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />

<!-- Logging Packages -->
<PackageReference Include="Serilog" Version="3.1.1" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
```

**Verify Package Integrity:**
```bash
# Check for package vulnerabilities
dotnet list package --vulnerable

# Check for outdated packages
dotnet list package --outdated

# Verify package sources
dotnet nuget list source
```

#### Step 5: Application Build and Initial Run

**Build the Application:**
```bash
# Build in Debug configuration
dotnet build --configuration Debug

# Build in Release configuration (for production)
dotnet build --configuration Release

# Build with specific runtime (optional)
dotnet build --runtime win-x64  # For Windows
dotnet build --runtime osx-x64   # For macOS Intel
dotnet build --runtime osx-arm64 # For macOS Apple Silicon
dotnet build --runtime linux-x64 # For Linux
```

**Run the Application:**
```bash
# Start the application in development mode
dotnet run

# Alternative: Run with specific environment
dotnet run --environment Development

# Run with custom URLs
dotnet run --urls "http://localhost:5000;https://localhost:5001"
```

**Expected Startup Output:**
```
Building...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Development
info: Microsoft.Hosting.Lifetime[0]
      Content root path: /path/to/GIDDH_UI_Sharp
```

#### Step 6: Application Verification and Testing

**Verify Application Endpoints:**

1. **Health Check Endpoint:**
   ```bash
   curl -X GET "http://localhost:5000/health" -H "accept: application/json"
   ```

2. **API Documentation:**
   - Open browser and navigate to: `http://localhost:5000/swagger`
   - Verify Swagger UI loads with API documentation
   - Check that PDF generation endpoint is visible

3. **Application Info:**
   ```bash
   curl -X GET "http://localhost:5000/info" -H "accept: application/json"
   ```

**Test Basic Functionality:**
```bash
# Test application responsiveness
curl -I http://localhost:5000

# Expected response headers should include:
# HTTP/1.1 200 OK
# Content-Type: text/html; charset=utf-8
# Server: Kestrel
```

### Post-Installation Configuration

#### Chrome Browser Configuration

**Verify Chrome Installation:**

1. **Windows:**
   ```cmd
   # Check Chrome installation path
   dir "C:\Program Files\Google\Chrome\Application\chrome.exe"
   
   # Verify Chrome version
   "C:\Program Files\Google\Chrome\Application\chrome.exe" --version
   ```

2. **macOS:**
   ```bash
   # Check Chrome installation
   ls -la "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"
   
   # Verify Chrome version
   "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" --version
   ```

3. **Linux:**
   ```bash
   # Check Chrome installation
   which google-chrome
   # or
   which chromium-browser
   
   # Verify Chrome version
   google-chrome --version
   ```

#### Environment-Specific Configuration

**Development Environment Setup:**
```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "PuppeteerSharp": "Information"
    }
  },
  "ChromePath": {
    "Windows": "C:/Program Files/Google/Chrome/Application/chrome.exe",
    "macOS": "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
    "Linux": "/usr/bin/google-chrome"
  },
  "PdfGeneration": {
    "OutputDirectory": "./Downloads",
    "TempDirectory": "./temp",
    "MaxConcurrentGenerations": 5
  }
}
```

**Production Environment Setup:**
```json
// appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ChromePath": "/usr/bin/google-chrome",
  "PdfGeneration": {
    "OutputDirectory": "/app/downloads",
    "TempDirectory": "/tmp",
    "MaxConcurrentGenerations": 10
  }
}
```

---

## PDF Generation Guide

### Comprehensive PDF Generation Workflow

This section provides a complete, step-by-step guide for generating PDFs using the GiddhTemplate service. The process involves capturing data from the Giddh application, configuring the local development environment, and executing API calls to generate high-quality PDF documents.

### Prerequisites for PDF Generation

#### Essential Requirements
- **.NET 8.0 SDK**: Successfully installed and configured
- **Google Chrome Browser**: Version 90+ installed and accessible
- **Giddh Application Access**: Valid credentials for books.giddh.com or test.giddh.com
- **API Testing Tool**: Postman, Insomnia, or curl command-line tool
- **Network Access**: Stable internet connection for API calls
- **Local Development Environment**: GiddhTemplate application running locally

#### Development Tools Setup
- **Postman**: Download and install from https://www.postman.com/downloads/
- **Browser Developer Tools**: Familiarity with Chrome DevTools for network inspection
- **JSON Editor**: For payload modification and validation
- **File System Access**: Permissions to read/write in the Downloads directory

### Detailed Step-by-Step PDF Generation Process

#### Step 1: Access Giddh Application Environment

**Production Environment Access:**
- **URL**: https://books.giddh.com
- **Purpose**: Live production data and real invoice generation
- **Authentication**: Use your production Giddh credentials
- **Data Considerations**: Real customer data, use with caution

**Test Environment Access:**
- **URL**: https://test.giddh.com
- **Purpose**: Safe testing environment with sample data
- **Authentication**: Use your test environment credentials
- **Data Considerations**: Safe for experimentation and development

**Login Process:**
1. Navigate to the chosen environment URL
2. Enter your username and password
3. Complete any two-factor authentication if enabled
4. Verify successful login by checking dashboard access

#### Step 2: Navigate to Invoice and Prepare for PDF Preview

**Locate Target Invoice:**
1. **Dashboard Navigation**: Go to the main dashboard
2. **Invoice Section**: Navigate to Sales → Invoices or similar menu
3. **Invoice Selection**: Choose any existing invoice for PDF generation
4. **Invoice Types Supported**:
   - Standard Invoices
   - Tax Invoices
   - Proforma Invoices
   - Credit Notes
   - Debit Notes
   - Purchase Orders
   - Purchase Bills
   - Receipt Vouchers
   - Payment Vouchers

**Alternative Navigation Methods:**
- Use the search functionality to find specific invoices
- Filter invoices by date range, customer, or amount
- Access recent invoices from the dashboard quick links

#### Step 3: Capture Network Traffic for API Analysis

**Open Browser Developer Tools:**
1. **Windows/Linux**: Press `F12` or `Ctrl+Shift+I`
2. **macOS**: Press `Cmd+Option+I`
3. **Alternative**: Right-click on page → "Inspect" → "Network" tab

**Configure Network Monitoring:**
1. **Clear Previous Requests**: Click the clear button (🚫) in Network tab
2. **Enable Request Logging**: Ensure "Preserve log" is checked
3. **Filter Settings**: Set filter to "XHR" or "Fetch" to see API calls
4. **Response Body**: Enable "Response" column to see response data

**Trigger PDF Preview:**
1. **Locate PDF Button**: Find "Preview PDF" or "Download PDF" button on invoice
2. **Click PDF Action**: Click the button to initiate PDF generation
3. **Monitor Network**: Watch for new network requests in DevTools
4. **Identify Target Request**: Look for `/download-file` API endpoint

**Expected Network Request Pattern:**
```
Request URL: https://api.giddh.com/company/{companyId}/accounts/{accountId}/vouchers/download-file
Request Method: POST
Content-Type: application/json
Authorization: Bearer {jwt_token}
```

#### Step 4: Extract and Modify API Request

**Capture cURL Command:**
1. **Right-click on Request**: Find the `/download-file` request in Network tab
2. **Copy as cURL**: Right-click → Copy → Copy as cURL (bash)
3. **Save to File**: Paste the cURL command into a text editor
4. **Verify Completeness**: Ensure headers and request body are included

**Example Captured cURL:**
```bash
curl 'https://api.giddh.com/company/example123/accounts/sales/vouchers/download-file' \
  -H 'accept: application/json' \
  -H 'authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...' \
  -H 'content-type: application/json' \
  --data-raw '{"voucherNumber":"INV-001","voucherType":"INVOICE"}'
```

**Add Debug Mode Parameter:**
Modify the request URL or request body to include debug mode:

**Method 1 - URL Parameter:**
```bash
curl 'https://api.giddh.com/company/example123/accounts/sales/vouchers/download-file?debugMode=true' \
  # ... rest of the curl command
```

**Method 2 - Request Body:**
```bash
curl 'https://api.giddh.com/company/example123/accounts/sales/vouchers/download-file' \
  # ... headers ...
  --data-raw '{"voucherNumber":"INV-001","voucherType":"INVOICE","debugMode":true}'
```

#### Step 5: Execute Modified API Request and Extract Payload

**Execute cURL Command:**
1. **Open Terminal/Command Prompt**: Access command line interface
2. **Paste Modified Command**: Execute the cURL with debugMode=true
3. **Capture Response**: Save the complete JSON response to a file
4. **Verify Response**: Ensure the response contains complete invoice data

**Expected Response Structure:**
The response should contain a comprehensive JSON object with all invoice details:

```json
{
  "templateType": "TemplateA",
  "voucherNumber": "INV-001",
  "voucherDate": "23-12-2025",
  "voucherType": "INVOICE",
  "company": { /* company details */ },
  "customerDetails": { /* customer information */ },
  "entries": [ /* invoice line items */ ],
  "theme": { /* styling and layout */ },
  "settings": { /* display preferences */ }
  // ... additional fields
}
```

**Save Payload for Testing:**
1. **Create Test File**: Save response as `test-payload.json`
2. **Validate JSON**: Use online JSON validator or `jq` tool
3. **Format JSON**: Pretty-print for readability
4. **Backup Original**: Keep original response for reference

#### Step 6: Configure Chrome Path for Local Development

**Locate PdfService.cs File:**
```bash
# Navigate to Services directory
cd Services

# Open PdfService.cs in your preferred editor
code PdfService.cs  # VS Code
# or
vim PdfService.cs   # Vim
# or
nano PdfService.cs  # Nano
```

**Find Chrome Configuration Section:**
Locate the `GetBrowserAsync()` method around lines 38-45:

```csharp
var launchOptions = new LaunchOptions
{
    Headless = true,
    ExecutablePath = "/usr/bin/google-chrome", // Server Google Chrome path
    // ExecutablePath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", // Local path MacOS
    // ExecutablePath = "C:/Program Files/Google/Chrome/Application/chrome.exe", // Local path Windows
    Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--lang=en-US,ar-SA" }
};
```

**Configure for Your Operating System:**

**For Windows Development:**
```csharp
var launchOptions = new LaunchOptions
{
    Headless = true,
    // ExecutablePath = "/usr/bin/google-chrome", // Server Google Chrome path
    // ExecutablePath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", // Local path MacOS
    ExecutablePath = "C:/Program Files/Google/Chrome/Application/chrome.exe", // Local path Windows
    Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--lang=en-US,ar-SA" }
};
```

**For macOS Development:**
```csharp
var launchOptions = new LaunchOptions
{
    Headless = true,
    // ExecutablePath = "/usr/bin/google-chrome", // Server Google Chrome path
    ExecutablePath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", // Local path MacOS
    // ExecutablePath = "C:/Program Files/Google/Chrome/Application/chrome.exe", // Local path Windows
    Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--lang=en-US,ar-SA" }
};
```

**For Linux Development:**
```csharp
var launchOptions = new LaunchOptions
{
    Headless = true,
    ExecutablePath = "/usr/bin/google-chrome", // Server Google Chrome path (keep as is)
    // ExecutablePath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", // Local path MacOS
    // ExecutablePath = "C:/Program Files/Google/Chrome/Application/chrome.exe", // Local path Windows
    Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--lang=en-US,ar-SA" }
};
```

**Alternative Chrome Paths:**
If Chrome is installed in a non-standard location, use these alternatives:

```csharp
// Windows alternatives
"C:/Program Files (x86)/Google/Chrome/Application/chrome.exe"
"C:/Users/{username}/AppData/Local/Google/Chrome/Application/chrome.exe"

// macOS alternatives
"/usr/local/bin/chrome"
"/opt/homebrew/bin/chrome"

// Linux alternatives
"/usr/bin/chromium-browser"
"/usr/bin/chromium"
"/snap/bin/chromium"
"/opt/google/chrome/chrome"
```

#### Step 7: Build and Run the Application

**Stop Any Running Instances:**
```bash
# If application is already running, stop it
# Press Ctrl+C in the terminal where it's running

# Or kill by port (if needed)
# Windows:
netstat -ano | findstr :5000
taskkill /PID {process_id} /F

# macOS/Linux:
lsof -ti:5000 | xargs kill -9
```

**Build the Application:**
```bash
# Clean previous builds
dotnet clean

# Restore dependencies
dotnet restore

# Build the application
dotnet build --configuration Debug

# Verify build success
echo $?  # Should return 0 on success
```

**Run the Application:**
```bash
# Start the application
dotnet run

# Alternative with specific environment
dotnet run --environment Development

# Alternative with custom port
dotnet run --urls "http://localhost:5000"
```

**Verify Application Startup:**
Look for these log messages:
```
Building...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

**Test Application Health:**
```bash
# Test basic connectivity
curl -I http://localhost:5000

# Expected response:
# HTTP/1.1 200 OK
# Content-Type: text/html; charset=utf-8
# Server: Kestrel
```

#### Step 8: Configure and Execute PDF Generation API Request

**Prepare Postman Collection:**
1. **Open Postman**: Launch the Postman application
2. **Create New Request**: Click "New" → "HTTP Request"
3. **Set Request Method**: Change from GET to POST
4. **Set Request URL**: Enter `http://localhost:5000/api/v1/pdf`

**Configure Request Headers:**
```
Content-Type: application/json
Accept: application/json
```

**Set Request Body:**
1. **Select Body Tab**: Click on "Body" tab in Postman
2. **Choose Raw**: Select "raw" radio button
3. **Set JSON Format**: Choose "JSON" from dropdown
4. **Paste Payload**: Copy the JSON payload from Step 5

**Alternative cURL Command:**
```bash
curl -X POST "http://localhost:5000/api/v1/pdf" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d @test-payload.json \
  --output generated-invoice.pdf
```

**Execute the Request:**
1. **Click Send**: Execute the request in Postman
2. **Monitor Response**: Watch for response status and timing
3. **Check Response Headers**: Verify Content-Type is application/pdf
4. **Download PDF**: Save the response as a PDF file

#### Step 9: Verify PDF Generation Results

**Check Response Status:**
- **Success**: HTTP 200 OK status code
- **Content-Type**: application/pdf
- **Content-Length**: Should be > 0 bytes
- **Response Time**: Typically 2-10 seconds depending on complexity

**Verify PDF File:**
1. **Download Location**: Check `/Downloads` directory in project root (development environment only)
2. **File Size**: Verify PDF file is not empty (> 1KB)
3. **File Integrity**: Open PDF in a PDF viewer
4. **Content Verification**: Ensure all invoice data is rendered correctly
5. **File Naming**: Verify custom filename from `pdfRename` parameter is applied correctly
6. **Duplicate Handling**: Check that duplicate files are numbered appropriately (e.g., `filename_1.pdf`)

**Expected PDF Features:**
- **Header Section**: Company logo, name, and contact information
- **Invoice Details**: Invoice number, date, due date, customer information
- **Line Items**: Product/service details with quantities, rates, and amounts
- **Tax Calculations**: Proper GST, TDS, TCS calculations if applicable
- **Footer Section**: Terms, conditions, and signature areas
- **Styling**: Applied theme colors, fonts, and margins
- **Custom Naming**: PDF files named according to `pdfRename` parameter or default timestamp format
- **Environment-Based Saving**: Automatic file saving in development environments only

**Quality Checks:**
- **Text Rendering**: All text should be clear and readable
- **Image Quality**: Logos and signatures should be crisp
- **Layout**: Proper alignment and spacing
- **Page Breaks**: Appropriate pagination for multi-page invoices
- **QR Codes**: Should be scannable if present

#### Step 10: Generate Additional PDFs and Batch Processing

**Single PDF Generation:**
```bash
# Generate individual PDFs with different payloads
curl -X POST "http://localhost:5000/api/v1/pdf" \
  -H "Content-Type: application/json" \
  -d @payload1.json \
  --output invoice1.pdf

curl -X POST "http://localhost:5000/api/v1/pdf" \
  -H "Content-Type: application/json" \
  -d @payload2.json \
  --output invoice2.pdf
```

**Batch Processing Script:**
```bash
#!/bin/bash
# batch-generate-pdfs.sh

for payload_file in payloads/*.json; do
    filename=$(basename "$payload_file" .json)
    echo "Generating PDF for $filename..."
    
    curl -X POST "http://localhost:5000/api/v1/pdf" \
      -H "Content-Type: application/json" \
      -d @"$payload_file" \
      --output "output/${filename}.pdf" \
      --silent
    
    if [ $? -eq 0 ]; then
        echo "✓ Successfully generated ${filename}.pdf"
    else
        echo "✗ Failed to generate ${filename}.pdf"
    fi
done
```

**PowerShell Batch Script (Windows):**
```powershell
# batch-generate-pdfs.ps1

$payloadFiles = Get-ChildItem -Path "payloads" -Filter "*.json"

foreach ($file in $payloadFiles) {
    $filename = $file.BaseName
    Write-Host "Generating PDF for $filename..."
    
    $response = Invoke-RestMethod -Uri "http://localhost:5000/api/v1/pdf" `
        -Method POST `
        -ContentType "application/json" `
        -InFile $file.FullName `
        -OutFile "output\$filename.pdf"
    
    if ($?) {
        Write-Host "✓ Successfully generated $filename.pdf" -ForegroundColor Green
    } else {
        Write-Host "✗ Failed to generate $filename.pdf" -ForegroundColor Red
    }
}
```

### Advanced PDF Generation Features

#### Template-Specific Generation

**TemplateA (Standard Business Templates):**
- **Use Cases**: Standard invoices, quotations, purchase orders
- **Features**: Full customization, multiple voucher types, comprehensive layouts
- **Payload Requirements**: Complete company and customer details

**Tally Template:**
- **Use Cases**: Tally ERP integration, accounting-specific formats
- **Features**: Tally-compatible layouts, specialized tax calculations
- **Payload Requirements**: Tally-specific field mappings

**Thermal Template:**
- **Use Cases**: Receipt printing, POS systems, compact layouts
- **Features**: Optimized for thermal printers, minimal styling
- **Payload Requirements**: Essential transaction details only

#### Dynamic Theme Application

**Color Scheme Customization:**
```json
{
  "theme": {
    "primaryColor": "#2563eb",
    "secondaryColor": "#f8fafc",
    "font": {
      "family": "Inter",
      "fontSizeDefault": 14,
      "fontSizeMedium": 12,
      "fontSizeSmall": 10
    },
    "margin": {
      "top": 20,
      "right": 20,
      "bottom": 20,
      "left": 20
    }
  }
}
```

#### Multi-Language Support

**Language Configuration:**
```json
{
  "language": "en-US",
  "currencyFormat": "SYMBOL",
  "dateFormat": "DD-MM-YYYY",
  "numberFormat": "en-US"
}
```

### Performance Optimization Tips

#### Concurrent PDF Generation
- **Default Limit**: 1 concurrent generation (semaphore)
- **Recommended**: Increase for production environments
- **Configuration**: Modify `_semaphore` initialization in PdfService.cs

#### Memory Management
- **Browser Reuse**: Single browser instance for multiple PDFs
- **Page Cleanup**: Automatic page disposal after generation
- **Memory Monitoring**: Watch for memory leaks in long-running processes

#### Caching Strategies
- **Font Caching**: Fonts are cached after first load
- **Template Caching**: Compiled Razor templates are cached
- **Chrome Binary**: Browser instance is reused across requests
---

## Complete API Documentation

### API Overview

The GiddhTemplate service provides a comprehensive RESTful API for PDF generation with a single primary endpoint that handles all template types and customization options.

#### Base URL
- **Development**: `http://localhost:5000`
- **Production**: `https://your-domain.com`

#### API Versioning
- **Current Version**: v1
- **Base Path**: `/api/v1`

### Primary PDF Generation Endpoint

#### POST /api/v1/pdf

**Description**: Generates a PDF document from provided invoice/voucher data using specified template and styling options.

**Request Specifications:**
- **Method**: POST
- **Content-Type**: application/json
- **Accept**: application/pdf
- **Maximum Payload Size**: 10MB
- **Timeout**: 30 seconds

**Request Headers:**
```http
POST /api/v1/pdf HTTP/1.1
Host: localhost:5000
Content-Type: application/json
Accept: application/pdf
Content-Length: {payload_size}
```

**Response Specifications:**
- **Success Status**: 200 OK
- **Content-Type**: application/pdf
- **Response Body**: Binary PDF data
- **File Size**: Varies (typically 100KB - 5MB)

**Success Response Headers:**
```http
HTTP/1.1 200 OK
Content-Type: application/pdf
Content-Length: {pdf_size}
Content-Disposition: attachment; filename="invoice.pdf"
Cache-Control: no-cache
```

**Error Response Format:**
```json
{
  "error": {
    "code": "PDF_GENERATION_FAILED",
    "message": "Detailed error description",
    "details": {
      "templateType": "TemplateA",
      "stage": "template_rendering",
      "innerException": "Chrome process failed to start"
    },
    "timestamp": "2025-12-23T12:17:00Z",
    "requestId": "req_123456789"
  }
}
```

### Error Codes and Status Codes

#### HTTP Status Codes
- **200 OK**: PDF generated successfully
- **400 Bad Request**: Invalid request payload or parameters
- **422 Unprocessable Entity**: Valid JSON but invalid business logic
- **500 Internal Server Error**: Server-side processing error
- **503 Service Unavailable**: Chrome browser unavailable or system overloaded
- **408 Request Timeout**: PDF generation exceeded timeout limit

#### Application Error Codes
- **INVALID_TEMPLATE_TYPE**: Unsupported template type specified
- **MISSING_REQUIRED_FIELD**: Required field missing from payload
- **CHROME_LAUNCH_FAILED**: Unable to start Chrome browser process
- **TEMPLATE_RENDERING_FAILED**: Razor template compilation or rendering error
- **PDF_GENERATION_FAILED**: Chrome PDF generation process failed
- **FONT_LOADING_FAILED**: Unable to load specified font family
- **INVALID_THEME_CONFIG**: Theme configuration contains invalid values
- **CONCURRENT_LIMIT_EXCEEDED**: Too many concurrent PDF generation requests


---

## Payload Structure and Examples

### Complete JSON Payload Structure

The PDF generation API accepts comprehensive JSON payloads containing all invoice/voucher data, styling preferences, and configuration options. Below are complete examples for each template type.

#### TemplateA Complete Payload Example

```json
{
  "templateType": "TemplateA",
  "voucherNumber": "B-251223-1",
  "voucherDate": "23-12-2025",
  "voucherType": "INVOICE",
  "dueDate": "23-12-2025",
  "shippingDate": "24-12-2025",
  "customField1": "Custom F1",
  "customField2": "Custom F2",
  "customField3": "Custom F3",
  "shippedVia": "Truck",
  "stockQuantityWithUnit": "2 btl",
  "attentionTo": "Divyanshu Ji",
  "trackingNumber": "MP091999",
  "placeOfSupply": "Ladakh",
  "currencyFormat": "SYMBOL",
  "accountCurrency": {
    "code": "INR",
    "symbol": "&#x20b9;"
  },
  "company": {
    "name": "(trigger) New Voucher Logic Unregistered",
    "headerCompanyName": "(trigger) New Voucher Logic Unregistered",
    "footerCompanyName": "(trigger) New Voucher Logic Unregistered",
    "address": "New address",
    "defaultAddress": "New address",
    "contactNumber": "91-9864531232",
    "email": "",
    "taxNumber": "29MNBHJ3232A3Z3",
    "logo": {
      "url": "https://apitest.giddh.com/company/newvouin16255749585340xoofk/image/gog835inhp1766467964943",
      "size": "50px"
    },
    "currency": {
      "code": "INR",
      "symbol": "&#x20b9;"
    },
    "billing": {
      "country": "India",
      "address": "New address",
      "taxNumber": "29MNBHJ3232A3Z3",
      "stateCounty": "Karnataka"
    },
    "shipping": {
      "country": "India",
      "address": "New address",
      "taxNumber": "29MNBHJ3232A3Z3"
    }
  },
  "customerDetails": {
    "name": "Ankit Dubey",
    "contactNumber": "919111525164",
    "email": "divyanshu@walkover.in"
  },
  "customerName": "Ankit Dubey",
  "customerEmail": "divyanshu@walkover.in",
  "customerMobileNumber": "919111525164",
  "billing": {
    "country": "India",
    "address": "",
    "taxNumber": "",
    "stateCounty": "Ladakh"
  },
  "shipping": {
    "country": "India",
    "address": "",
    "taxNumber": "",
    "stateCounty": "Ladakh"
  },
  "warehouseDetails": {
    "name": "Branch 1 warehouse",
    "address": ""
  },
  "theme": {
    "marginEnable": true,
    "margin": {
      "top": 20.0,
      "right": 20.0,
      "bottom": 0.0,
      "left": 20.0
    },
    "primaryColor": "#f63407",
    "secondaryColor": "#ffffff",
    "font": {
      "family": "Roboto",
      "fontSizeDefault": 14,
      "fontSizeMedium": 12,
      "fontSizeSmall": 10
    }
  },
  "entries": [
    {
      "date": "23-12-2025",
      "accountName": "Sales",
      "description": "Particular description at time voucher creation",
      "amount": {
        "amountForAccount": 1000.000,
        "amountForCompany": 1000.000
      },
      "taxableValue": {
        "amountForAccount": 500.000,
        "amountForCompany": 500.000
      },
      "subTotal": {
        "amountForAccount": 1000.000,
        "amountForCompany": 1000.000
      },
      "discounts": [
        {
          "calculationMethod": "PERCENTAGE",
          "accountName": "Discount",
          "accountUniqueName": "discount",
          "amount": {
            "amountForAccount": 500.000,
            "amountForCompany": 500.000
          },
          "discountValue": 50
        }
      ],
      "discountTotal": {
        "amountForAccount": 500.000,
        "amountForCompany": 500.000
      },
      "taxes": [
        {
          "taxType": "gst",
          "calculationMethod": "OnTaxableAmount",
          "accountName": "IGST",
          "accountUniqueName": "igst",
          "taxAccountUniqueName": "igst",
          "uniqueName": "gst12",
          "amount": {
            "amountForAccount": 60.000,
            "amountForCompany": 60.000
          },
          "taxPercent": 12.00,
          "considerInItemTotal": true,
          "considerInVoucherTotal": true,
          "showOnVoucher": true
        }
      ],
      "taxTotal": {
        "amountForAccount": 60.000,
        "amountForCompany": 60.000
      },
      "entryTotal": {
        "amountForAccount": 560.000,
        "amountForCompany": 560.000
      },
      "grandTotal": {
        "amountForAccount": 560.000,
        "amountForCompany": 560.000
      },
      "sumOfDiscounts": {
        "amountForCompany": 500.000,
        "amountForAccount": 500.000
      },
      "salesPerson": {
        "name": "Saurabh",
        "uniqueName": "divyanshuarchived",
        "email": "",
        "mobileNumber": "919111525164"
      },
      "usedQuantity": 0,
      "stock": null
    }
  ],
  "taxableAmount": {
    "amountForAccount": 1220.000,
    "amountForCompany": 1220.000
  },
  "taxableTotal": {
    "amountForAccount": 720.000,
    "amountForCompany": 720.000
  },
  "totalTax": {
    "amountForAccount": 121.600,
    "amountForCompany": 121.600
  },
  "grandTotal": {
    "amountForAccount": 832.000,
    "amountForCompany": 832.000
  },
  "totalDue": {
    "amountForAccount": 0.00,
    "amountForCompany": 0.00
  },
  "paidAmount": {
    "amountForAccount": 832.00,
    "amountForCompany": 0
  },
  "balance": {
    "amountForAccount": 0.00,
    "amountForCompany": 0.00
  },
  "roundOff": "0.400",
  "totalInWords": {
    "amountForAccount": "Eight Hundred Forty Two Only",
    "amountForCompany": "Eight Hundred Forty Two Only"
  },
  "QRCodeBase64String": "iVBORw0KGgoAAAANSUhEUgAAASwAAAEsAQAAAABRBrPYAAADgElEQVR4Xu2ZTa6bShCFy/KAIUtgJ2ZjlrDExmAnLMFDBojK+aq5jk2e9DKsVtzSRRg+Rzquv9Md879ZTzs/+c/1xU7ri53WFzut9Nhi",
  "bankQRDetails": {
    "bankQRCodeBase64": "iVBORw0KGgoAAAANSUhEUgAAAZAAAAGQAQAAAACoxAthAAADXUlEQVR4Xu3XQW7bMBAFUBpaeKkj6Cg6mnU0Aj1IfQQvvRDC8v9PUqrjxBzHKYpgCEGgh/8xtTSW3ZDMI9wWHo8QQx1DyvN5DXMur6XS"
  },
  "showBankQR": true,
  "imageSignature": "https://apitest.giddh.com/company/newvouin16255749585340xoofk/image/1766488068067s62euw0eao",
  "sealPath": "https://apitest.giddh.com/seal/paid",
  "message1": "This is note which created at time to template create or update.",
  "message2": "This note written at time of voucher creation",
  "isTaxesApplied": true,
  "isBusinessToCustomerInvoice": true,
  "isBusinessToBusinessInvoice": false,
  "isMultipleCurrency": false,
  "displayBaseCurrency": true,
  "displayPlaceOfSupply": true,
  "showSectionsInline": false,
  "showVariantImage": false,
  "showSplittedDate": true,
  "showDueDate": true,
  "showDueMonth": null,
  "reversecharge": false,
  "companyTaxType": "GSTIN",
  "accountTaxType": "GSTIN",
  "billingCountyOrStateLabel": "state",
  "shippingCountyOrStateLabel": "state"
}
```

---

## Architecture and Technical Details

### Dependencies
```xml
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.11" />
<PackageReference Include="PuppeteerSharp" Version="20.1.1" />
<PackageReference Include="RazorLight" Version="2.3.1" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
<PackageReference Include="Serilog" Version="3.1.1" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
```

### API Endpoints

#### PDF Generation
```http
POST /api/v1/pdf
Content-Type: application/json

{
  "templateType": "TemplateA",
  "voucherType": "Invoice",
  "theme": { ... },
  "data": { ... }
}
```

### Template Types

#### TemplateA (Default)
- Standard invoice and document templates
- Support for various voucher types (Invoice, Receipt, Payment, etc.)
- Customizable headers, footers, and body sections

#### Tally
- Tally ERP integration templates
- Specialized formatting for Tally data structures

#### Thermal
- Thermal printer optimized templates
- Compact layout for receipt printing

### Theming Support
- **Font Families**: Inter, Open Sans, Lato, Roboto
- **Dynamic Sizing**: Configurable font sizes
- **Color Schemes**: Primary and secondary color theming
- **Margins**: Customizable page margins

---

## Troubleshooting

### Setup Issues

#### 1. .NET SDK Not Found
```
'dotnet' is not recognized as an internal or external command
```
**Solution**: Ensure .NET 8.0 SDK is properly installed and added to your system PATH.

#### 2. Port Already in Use
```
Failed to bind to address http://localhost:5000: address already in use
```
**Solution**: 
- Kill any existing processes using port 5000
- Or modify the port in `launchSettings.json`

#### 3. Dependency Restore Fails
```
error NU1101: Unable to find package
```
**Solution**:
- Check internet connection
- Clear NuGet cache: `dotnet nuget locals all --clear`
- Retry: `dotnet restore`

#### 4. Build Errors
```
error CS0246: The type or namespace name could not be found
```
**Solution**:
- Ensure all dependencies are restored
- Check .NET version compatibility
- Clean and rebuild: `dotnet clean && dotnet build`

### PDF Generation Issues

#### 1. Chrome not found error
- Verify Chrome is installed
- Check the ExecutablePath is correct for your OS
- Ensure Chrome is accessible from the specified path

#### 2. PDF generation fails
- Verify the JSON payload is valid
- Check Chrome browser permissions
- Ensure all required fonts are available

#### 3. Template not found
- Verify template files exist in `/Templates` directory
- Check template type in the request payload

### System Requirements

**Minimum Requirements:**
- **OS**: Windows 10, macOS 10.15, or Linux (Ubuntu 18.04+)
- **RAM**: 4GB minimum, 8GB recommended
- **Storage**: 2GB free space
- **.NET**: Version 8.0 or higher
- **Browser**: Google Chrome (required for PDF generation)

---

## Development Guidelines

### Development Workflow

For ongoing development:

1. Make code changes
2. Stop the application (Ctrl+C)
3. Run `dotnet run` to restart with changes
4. Test your changes

### Debug Mode Benefits
Using `debugMode=true` in the original API call provides:
- Complete request payload structure
- All required fields for PDF generation
- proper data formatting examples

### Best Practices

1. **Code Changes**: Always restart the application after making code changes
2. **Testing**: Use Postman or similar tools for API testing
3. **Chrome Configuration**: Ensure correct Chrome path for your operating system
4. **Template Management**: Keep template files organized in respective directories
5. **Error Handling**: Check logs for detailed error information

### API Documentation
- Swagger UI available at: `http://localhost:5000/swagger`
- OpenAPI specification included

### Contributing Guidelines

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

---

## Support and Resources

### Documentation Files
- `README.md` - Project overview and quick start
- `docs/setup.md` - Detailed setup instructions
- `docs/pdf-generation.md` - PDF generation workflow
- `docs/controllers.md` - API controller documentation
- `docs/services.md` - Service layer documentation
- `docs/models.md` - Data model documentation
- `docs/templates.md` - Template structure documentation
- `docs/program.md` - Application configuration

### Related Projects
- **GIDDH**: Main accounting application
- **Walkover Web Solution**: Parent organization

### License
This project is part of the Walkover Web Solution ecosystem.

---

**Note**: This documentation covers the complete setup, usage, and development guidelines for the GiddhTemplate PDF generation service. For specific technical details, refer to the individual documentation files in the `docs/` directory.
