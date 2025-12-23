# GiddhTemplate - PDF Generation Service

A .NET 8.0 web application for generating PDF documents from HTML templates using PuppeteerSharp and Razor templating engine.

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK
- Google Chrome browser
- Git

### Installation
```bash
# Clone the repository
git clone https://github.com/Walkover-Web-Solution/GIDDH_UI_Sharp
cd GIDDH_UI_Sharp

# Restore dependencies
dotnet restore

# Run the application
dotnet run
```

The application will be available at `http://localhost:5000`

## 📚 Documentation

### Getting Started
- **[Setup Guide](docs/setup.md)** - First-time setup instructions
- **[PDF Generation Guide](docs/pdf-generation.md)** - Complete PDF generation workflow

### Architecture Documentation
- **[Program Configuration](docs/program.md)** - Application startup and configuration
- **[Controllers](docs/controllers.md)** - API endpoints and request handling
- **[Services](docs/services.md)** - Business logic and service implementations
- **[Models](docs/models.md)** - Data models and enums
- **[Templates](docs/templates.md)** - HTML template structure and styling

### Developer Resources
- **[Developer Guide](DEVELOPER_GUIDE.md)** - Advanced development information

## 🏗️ Project Structure

```
GiddhTemplate/
├── Controllers/           # API controllers
├── Models/               # Data models and enums
├── Services/             # Business logic services
├── Templates/            # HTML templates and fonts
│   ├── Fonts/           # Font files for PDF generation
│   ├── Tally/           # Tally template files
│   ├── TemplateA/       # Default template files
│   └── Thermal/         # Thermal printer template files
├── docs/                # Documentation files
└── sample-payloads/     # Sample JSON payloads for testing
```

## 🔧 Key Features

- **PDF Generation**: Convert HTML templates to PDF using Chrome headless browser
- **Multiple Templates**: Support for Tally, TemplateA, and Thermal templates
- **Font Management**: Dynamic font loading and CSS generation
- **Razor Templating**: Server-side HTML generation with Razor syntax
- **RESTful API**: Simple HTTP API for PDF generation

## 📋 API Endpoints

### PDF Generation
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

### API Documentation
- Swagger UI: `http://localhost:5000/swagger`
- OpenAPI specification available

## 🛠️ Technology Stack

- **.NET 8.0** - Web framework
- **PuppeteerSharp** - Chrome automation for PDF generation
- **RazorLight** - Template engine
- **Serilog** - Structured logging
- **Swashbuckle** - API documentation

## 📦 Dependencies

```xml
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.11" />
<PackageReference Include="PuppeteerSharp" Version="20.1.1" />
<PackageReference Include="RazorLight" Version="2.3.1" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
<PackageReference Include="Serilog" Version="3.1.1" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
```

## 🚦 Getting Started Workflow

1. **Setup**: Follow the [Setup Guide](docs/setup.md) for initial installation
2. **Configuration**: Configure Chrome path in `Services/PdfService.cs` for local development
3. **Testing**: Use the [PDF Generation Guide](docs/pdf-generation.md) to test PDF creation
4. **Development**: Refer to architecture documentation for code understanding

## 🔍 Template Types

### TemplateA (Default)
- Standard invoice and document templates
- Support for various voucher types (Invoice, Receipt, Payment, etc.)
- Customizable headers, footers, and body sections

### Tally
- Tally ERP integration templates
- Specialized formatting for Tally data structures

### Thermal
- Thermal printer optimized templates
- Compact layout for receipt printing

## 🎨 Theming Support

- **Font Families**: Inter, Open Sans, Lato, Roboto
- **Dynamic Sizing**: Configurable font sizes
- **Color Schemes**: Primary and secondary color theming
- **Margins**: Customizable page margins

## 🐛 Troubleshooting

Common issues and solutions are documented in:
- [Setup Guide - Troubleshooting](docs/setup.md#troubleshooting)
- [PDF Generation Guide - Troubleshooting](docs/pdf-generation.md#troubleshooting)

## 📝 Development Notes

- Chrome executable path must be configured for local development
- Application restart required after code changes (`dotnet run`)
- Generated PDFs are saved to `/Downloads` directory
- Debug mode available via `debugMode=true` query parameter

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## 📄 License

This project is part of the Walkover Web Solution ecosystem.

## 🔗 Related Projects

- **GIDDH**: Main accounting application
- **Walkover Web Solution**: Parent organization

---

For detailed information on any component, refer to the specific documentation files in the `docs/` directory.
