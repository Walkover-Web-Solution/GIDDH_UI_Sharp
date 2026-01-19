# First Time Setup Guide

This guide provides step-by-step instructions for setting up the GiddhTemplate project for the first time.

## Prerequisites

Before starting, ensure you have:
- Git installed on your system
- Internet connection for downloading dependencies

## Setup Steps

### Step 1: Clone the Repository
Clone the project repository from GitHub:
```bash
git clone https://github.com/Walkover-Web-Solution/GIDDH_UI_Sharp
```

### Step 2: Install .NET 8.0
Download and install .NET 8.0 SDK from the official Microsoft website:
- **Download Link**: https://dotnet.microsoft.com/download/dotnet/8.0
- Choose the appropriate installer for your operating system:
  - **Windows**: Download the Windows x64 installer
  - **macOS**: Download the macOS installer
  - **Linux**: Follow the distribution-specific installation instructions

**Verify Installation:**
After installation, verify .NET is properly installed by running:
```bash
dotnet --version
```
You should see version 8.0.x displayed.

### Step 3: Navigate to Project Directory
Change to the project directory:
```bash
cd GIDDH_UI_Sharp
```

### Step 4: Restore Dependencies
Install all project dependencies using:
```bash
dotnet restore
```

This command will:
- Download all NuGet packages specified in `GiddhTemplate.csproj`
- Restore project dependencies including:
  - PuppeteerSharp (v20.1.1)
  - RazorLight (v2.3.1)
  - Microsoft.AspNetCore.OpenApi (v8.0.11)
  - Swashbuckle.AspNetCore (v6.6.2)
  - Serilog packages for logging

### Step 5: Run the Application
Start the application:
```bash
dotnet run
```

**Expected Output:**
```
Building...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

The application will be available at: `http://localhost:5000`

### Step 6: PDF Generation Setup
For PDF generation functionality, refer to the detailed guide:
- **File**: `PDF_GENERATION_GUIDE.md`
- **Location**: Project root directory

## Project Structure

After successful setup, your project structure should include:

```
GIDDH_UI_Sharp/
├── Controllers/
│   └── PdfController.cs
├── Models/
│   ├── Enums/
│   └── RequestModel.cs
├── Services/
│   ├── PdfService.cs
│   ├── RazorTemplateService.cs
│   └── SlackService.cs
├── Templates/
│   ├── Fonts/
│   ├── Tally/
│   ├── TemplateA/
│   └── Thermal/
├── GiddhTemplate.csproj
├── Program.cs
├── PDF_GENERATION_GUIDE.md
└── SETUP_GUIDE.md
```

## Verification

### Test the Application
1. Open your browser and navigate to `http://localhost:5000`
2. You should see the application running
3. API documentation should be available at `http://localhost:5000/swagger`

### API Endpoints
The application provides the following main endpoint:
- **PDF Generation**: `POST /api/v1/pdf`

## Troubleshooting

### Common Issues

**1. .NET SDK Not Found**
```
'dotnet' is not recognized as an internal or external command
```
**Solution**: Ensure .NET 8.0 SDK is properly installed and added to your system PATH.

**2. Port Already in Use**
```
Failed to bind to address http://localhost:5000: address already in use
```
**Solution**: 
- Kill any existing processes using port 5000
- Or modify the port in `launchSettings.json`

**3. Dependency Restore Fails**
```
error NU1101: Unable to find package
```
**Solution**:
- Check internet connection
- Clear NuGet cache: `dotnet nuget locals all --clear`
- Retry: `dotnet restore`

**4. Build Errors**
```
error CS0246: The type or namespace name could not be found
```
**Solution**:
- Ensure all dependencies are restored
- Check .NET version compatibility
- Clean and rebuild: `dotnet clean && dotnet build`

### System Requirements

**Minimum Requirements:**
- **OS**: Windows 10, macOS 10.15, or Linux (Ubuntu 18.04+)
- **RAM**: 4GB minimum, 8GB recommended
- **Storage**: 2GB free space
- **.NET**: Version 8.0 or higher
- **Browser**: Google Chrome (required for PDF generation)

## Next Steps

After successful setup:

1. **Explore the API**: Visit `http://localhost:5000/swagger` for API documentation
2. **Test PDF Generation**: Follow the `PDF_GENERATION_GUIDE.md` for detailed PDF generation instructions
3. **Development**: Start making changes to the codebase as needed

## Development Workflow

For ongoing development:

1. Make code changes
2. Stop the application (Ctrl+C)
3. Run `dotnet run` to restart with changes
4. Test your changes

## Support

If you encounter issues not covered in this guide:
1. Check the project's GitHub repository for issues and documentation
2. Verify all prerequisites are met
3. Ensure you're using the correct .NET version (8.0)

---

**Note**: This setup guide covers the basic installation. For advanced configuration and PDF generation features, refer to `PDF_GENERATION_GUIDE.md`.
