# Templates Documentation

## Overview
The Templates directory contains invoice templates, styling, and fonts for generating customized PDF invoices. Each template supports different business requirements and output formats.

## Template Structure

### Directory Layout
```
Templates/
├── Fonts/                 # Font files for all templates
│   ├── Inter/             # Inter font family
│   ├── OpenSans/          # Open Sans font family
│   ├── Roboto/            # Roboto font family
│   └── Lato/              # Lato font family
├── TemplateA/             # Standard business template
│   ├── Header.cshtml      # Header section
│   ├── Body.cshtml        # Main content
│   ├── Footer.cshtml      # Footer section
│   ├── PO_PB_Body.cshtml  # Purchase Order/Proforma body
│   ├── PO_PB_Header.cshtml # Purchase Order/Proforma header
│   ├── Receipt_Payment_Body.cshtml # Receipt/Payment body
│   └── Styles/            # CSS styling files
├── Tally/                 # Accounting software format
│   ├── Header.cshtml      # Header section
│   ├── Body.cshtml        # Main content
│   ├── Footer.cshtml      # Footer section
│   └── Styles/            # CSS styling files
└── Thermal/               # Receipt printer format
    ├── Body.cshtml        # Compact body layout
    └── Styles/            # Minimal styling
```

## Template Types

### 1. TemplateA - Standard Business Template

**Purpose:** Professional invoice template for standard business use

**Components:**
- `Header.cshtml` (`@/Users/divyanshu/walkover/GiddhTemplate/Templates/TemplateA/Header.cshtml`) - Company branding and invoice details
- `Body.cshtml` - Line items, calculations, and tax breakdown
- `Footer.cshtml` - Terms, conditions, and signatures
- `PO_PB_Body.cshtml` - Purchase Order/Proforma specific layout
- `PO_PB_Header.cshtml` - Purchase Order/Proforma header
- `Receipt_Payment_Body.cshtml` - Receipt and payment voucher layout

**Header Features:**
```razor
@model InvoiceData.Root;
@using InvoiceData;

<header id="header">
    <table>
        <tbody>
            @* Copy type image for Invoice Copy variants *@
            @if(Model?.TypeOfCopy != null)
            {
                <tr>
                    <td class="p-0 text-center">
                        <img src="@Model.TypeOfCopy" height="40px" width="auto" />
                    </td>
                </tr>
            }
```

**Dynamic Elements:**
- Company logo with configurable size
- Conditional field display based on settings
- QR code integration for payments
- Tax number and GST composition display
- Multi-language support

### 2. Tally - Accounting Software Format

**Purpose:** Compatible with Tally accounting software requirements

**Components:**
- `Header.cshtml` - Simplified header layout
- `Body.cshtml` - Accounting-focused line items
- `Footer.cshtml` - Compliance and totals
- `Background.css` - Specialized background styling

**Key Features:**
- Tally-compatible field mapping
- Enhanced tax bifurcation display
- Accounting period compliance
- Background watermarks support

### 3. Thermal - Receipt Printer Format

**Purpose:** Optimized for thermal receipt printers

**Components:**
- `Body.cshtml` - Compact single-column layout
- Minimal styling for printer compatibility

**Characteristics:**
- Single-column layout
- Monospace font optimization
- Minimal graphics
- High contrast text
- Compact spacing

## Styling Architecture

### CSS Custom Properties (`@/Users/divyanshu/walkover/GiddhTemplate/Templates/TemplateA/Styles/Styles.css:1-50`)

**Font Variables:**
```css
@property --font-family {
  syntax: "<string>";
  inherits: true;
  initial-value: "Inter"; /* 'Roboto' | 'Open Sans' | 'Lato' | 'Inter' */
}

@property --font-size-default {
  syntax: "<length>";
  inherits: true;
  initial-value: 14px;
}

@property --font-size-medium {
  syntax: "<length>";
  inherits: true;
  initial-value: 12px;
}

@property --font-size-small {
  syntax: "<length>";
  inherits: true;
  initial-value: 10px;
}
```

**Color Variables:**
```css
@property --color-primary {
  syntax: "<color>";
  inherits: true;
  initial-value: #181b50;
}

@property --color-secondary {
  syntax: "<color>";
  inherits: true;
  initial-value: #6c757d;
}
```

**Font Weight Variables:**
```css
@property --font-weight-200 {
  syntax: "<integer>";
  inherits: true;
  initial-value: 200;
}

@property --font-weight-400 {
  syntax: "<integer>";
  inherits: true;
  initial-value: 400;
}

@property --font-weight-500 {
  syntax: "<integer>";
  inherits: true;
  initial-value: 500;
}

@property --font-weight-700 {
  syntax: "<integer>";
  inherits: true;
  initial-value: 700;
}
```

### Modular CSS Files

#### Styles.css - Global Variables and Base
- CSS custom properties
- Global reset styles
- Base typography
- Color scheme definitions

#### Header.css - Header Styling
- Logo positioning and sizing
- Company information layout
- Invoice metadata styling
- QR code positioning

#### Body.css - Content Area Styling
- Table layouts for line items
- Tax calculation displays
- Multi-column layouts
- Responsive adjustments

#### Footer.css - Footer Styling
- Terms and conditions formatting
- Signature areas
- Total calculations
- Contact information

#### Background.css (Tally only)
- Watermark positioning
- Background images
- Print-specific styling

## Font Management

### Supported Font Families

#### Inter (Default)
- **Path:** `Templates/Fonts/Inter/`
- **Weights:** 200, 400, 500, 700
- **Usage:** Modern UI-optimized font
- **Best For:** Digital invoices, clean layouts

#### Roboto
- **Path:** `Templates/Fonts/Roboto/`
- **Weights:** 200, 400, 500, 700
- **Usage:** Google Material Design
- **Best For:** Professional documents

#### Open Sans
- **Path:** `Templates/Fonts/OpenSans/`
- **Weights:** 200, 400, 500, 700
- **Usage:** Humanist sans-serif
- **Best For:** Readable body text

#### Lato
- **Path:** `Templates/Fonts/Lato/`
- **Weights:** 200, 400, 500, 700
- **Usage:** Friendly sans-serif
- **Best For:** Marketing materials

### Font Loading Process

**Dynamic CSS Generation:**
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

**Font Face Generation:**
```css
@font-face {
    font-family: 'Inter';
    src: url('data:font/woff2;base64,{base64data}') format('woff2');
    font-weight: 400;
    font-style: normal;
    font-display: swap;
}
```

## Template Rendering Process

### 1. Template Selection
```csharp
string templateType = request.TemplateType ?? "TemplateA";
string templatePath = Path.Combine("Templates", templateType);
```

### 2. Style Loading
```csharp
var styles = LoadStyles(templatePath);
// Returns: (Common, Header, Footer, Body, Background)
```

### 3. Font Processing
```csharp
string fontFamily = request.Theme?.Font?.Family ?? "Inter";
string fontCSS = LoadFontCSS(fontFamily);
```

### 4. HTML Compilation
```csharp
// Header rendering
string headerHtml = await _razorTemplateService.RenderTemplateAsync(
    Path.Combine(templatePath, "Header.cshtml"), request);

// Body rendering
string bodyHtml = await _razorTemplateService.RenderTemplateAsync(
    Path.Combine(templatePath, "Body.cshtml"), request);

// Footer rendering (if exists)
string footerHtml = await _razorTemplateService.RenderTemplateAsync(
    Path.Combine(templatePath, "Footer.cshtml"), request);
```

### 5. CSS Integration
```html
<style>
    {fontCSS}
    {styles.Common}
    {styles.Header}
    {styles.Body}
    {styles.Footer}
    {styles.Background}
</style>
```

## Razor Template Features

### Model Binding
```razor
@model InvoiceData.Root;
@using InvoiceData;
@using System.Collections.Generic;
```

### Conditional Rendering
```razor
@if (Model?.Settings?.ShowLogo?.Display == true &&
     !string.IsNullOrEmpty(Model?.Company?.Logo?.Url))
{
    <figure class="m-0">
        <img src="@Model?.Company?.Logo?.Url" 
             height="@Model?.Company?.Logo?.Size"
             width="auto" />
    </figure>
}
```

### Dynamic Styling
```razor
<td class="p-0 invoice-info vertical-align-bottom" 
    width="@(Model?.Settings?.ShowQrCode?.Display == true &&
             !string.IsNullOrEmpty(Model?.QRCodeBase64String) ? "30%" : "50%")">
```

### Loop Rendering
```razor
@if (Model?.Entries != null)
{
    @foreach (var entry in Model.Entries)
    {
        <tr>
            <td>@entry.AccountName</td>
            <td>@entry.Amount?.AmountForAccount</td>
        </tr>
    }
}
```

### Null Safety
```razor
@Model?.Settings?.CompanyTaxNumber?.Label
@(Model?.Company?.TaxNumber ?? "N/A")
```

## Customization Guidelines

### Adding New Templates

1. **Create Directory Structure:**
   ```
   Templates/NewTemplate/
   ├── Header.cshtml
   ├── Body.cshtml
   ├── Footer.cshtml (optional)
   └── Styles/
       ├── Styles.css
       ├── Header.css
       ├── Body.css
       └── Footer.css
   ```

2. **Update Template Selection Logic:**
   ```csharp
   // In PdfService.cs
   case "NewTemplate":
       templatePath = Path.Combine("Templates", "NewTemplate");
       break;
   ```

3. **Define CSS Variables:**
   ```css
   @property --custom-color {
     syntax: "<color>";
     inherits: true;
     initial-value: #custom;
   }
   ```

### Modifying Existing Templates

1. **CSS Customization:**
   - Modify CSS custom properties for global changes
   - Update specific CSS files for targeted changes
   - Maintain responsive design principles

2. **Razor Template Updates:**
   - Add new conditional sections
   - Modify existing layouts
   - Ensure null safety for new fields

3. **Font Integration:**
   - Add new font families to `Templates/Fonts/`
   - Update font loading logic in `PdfService.cs`
   - Define new font CSS variables

## Print Optimization

### PDF Generation Settings
- **Page Size:** A4 (210mm × 297mm)
- **Margins:** Configurable via Theme.Margin
- **Print Background:** Enabled for graphics
- **Scale:** 1.0 for accurate sizing

### CSS Print Media Queries
```css
@media print {
    .no-print { display: none; }
    .page-break { page-break-before: always; }
    body { -webkit-print-color-adjust: exact; }
}
```

### Performance Considerations
- **Image Optimization:** Base64 encoding for embedded images
- **Font Subsetting:** Only required glyphs included
- **CSS Minification:** Reduced file sizes
- **Template Caching:** Compiled template reuse

## Troubleshooting

### Common Issues

1. **Template Not Found:**
   - Verify template directory structure
   - Check case sensitivity in file names
   - Ensure all required files exist

2. **Styling Not Applied:**
   - Validate CSS syntax
   - Check CSS custom property definitions
   - Verify style loading order

3. **Font Not Loading:**
   - Confirm font files exist in correct directory
   - Validate font file formats (WOFF2 preferred)
   - Check font family name consistency

4. **Razor Compilation Errors:**
   - Verify model binding syntax
   - Check null safety operators
   - Validate using statements

### Debug Techniques

1. **HTML Output Inspection:**
   - Save generated HTML before PDF conversion
   - Validate HTML structure and CSS

2. **CSS Variable Testing:**
   - Use browser developer tools
   - Test CSS custom property inheritance

3. **Template Isolation:**
   - Test individual template components
   - Validate model data structure

---

**Author/Developer:** Divyanshu Shrivastava  
**Last Updated:** December 2025
