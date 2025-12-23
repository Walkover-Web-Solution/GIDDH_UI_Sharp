# Models Documentation

## Overview
The Models directory contains data transfer objects (DTOs) and enums that define the structure of invoice data and system configurations for the Giddh Template Service.

## Model Structure (`@/Users/divyanshu/walkover/GiddhTemplate/Models/RequestModel.cs`)

### 1. Root Model (`@/Users/divyanshu/walkover/GiddhTemplate/Models/RequestModel.cs:332-418`)

**Purpose:** Main container for complete invoice data

**Key Properties:**
```csharp
public class Root
{
    public Settings? Settings { get; set; }           // Display preferences
    public Company? Company { get; set; }             // Business information
    public Theme? Theme { get; set; }                 // Styling configuration
    public CustomerDetails? CustomerDetails { get; set; } // Client info
    public List<Entry>? Entries { get; set; }        // Invoice line items
    public Amount? GrandTotal { get; set; }           // Final totals
    public List<GstTaxesTotal>? GstTaxesTotal { get; set; } // Tax breakdown
    public VoucherTypeEnums VoucherTypeEnums { get; set; } // Invoice type
}
```

**Invoice Metadata:**
- `VoucherNumber` - Invoice identifier
- `VoucherDate` - Invoice creation date
- `DueDate` - Payment due date
- `PlaceOfSupply` - Tax jurisdiction
- `ExchangeRate` - Currency conversion rate

**Financial Data:**
- `TaxableAmount` - Pre-tax total
- `GrandTotal` - Final amount including taxes
- `TotalDue` - Outstanding balance
- `PreviousDueAmount` - Carried forward balance

### 2. Settings Model (`@/Users/divyanshu/walkover/GiddhTemplate/Models/RequestModel.cs:4-107`)

**Purpose:** Controls field visibility and display preferences

**Dynamic Configuration:**
```csharp
public Dictionary<string, Setting>? SettingDetails { get; set; }
```

**Key Display Settings:**
- `ShowLogo` - Company logo visibility
- `ShowQrCode` - QR code for payments
- `ShowEInvoiceDetails` - E-invoice compliance data
- `TaxBifurcation` - Tax breakdown display
- `ShowDescriptionInRows` - Item description formatting

**Initialization Method:**
```csharp
public void InitializeStaticKeys()
{
    if (SettingDetails == null) return;
    
    foreach (var property in GetType().GetProperties())
    {
        if (property.PropertyType == typeof(Setting) && property.CanWrite)
        {
            var key = property.Name.ToLower();
            property.SetValue(this, GetSetting(key));
        }
    }
}
```

### 3. Company Model (`@/Users/divyanshu/walkover/GiddhTemplate/Models/RequestModel.cs:142-156`)

**Purpose:** Business entity information and branding

**Structure:**
```csharp
public class Company
{
    public string? Name { get; set; }                 // Business name
    public string? Address { get; set; }              // Primary address
    public string? ContactNumber { get; set; }        // Phone number
    public string? Email { get; set; }                // Contact email
    public Currency? Currency { get; set; }           // Base currency
    public AddressDetails? Billing { get; set; }      // Billing address
    public AddressDetails? Shipping { get; set; }     // Shipping address
    public Logo? Logo { get; set; }                   // Brand logo
    public string? TaxNumber { get; set; }            // Tax registration
    public string? GstSchemeData { get; set; }        // GST scheme info
}
```

**Logo Configuration:**
```csharp
public class Logo
{
    public string? Url { get; set; }                  // Image URL/path
    public string? Size { get; set; }                 // Display size
}
```

### 4. Theme Model (`@/Users/divyanshu/walkover/GiddhTemplate/Models/RequestModel.cs:158-165`)

**Purpose:** Visual styling and layout configuration

**Structure:**
```csharp
public class Theme
{
    public bool? MarginEnable { get; set; }           // Custom margins
    public Margin? Margin { get; set; }               // Page margins
    public string? PrimaryColor { get; set; }         // Brand color
    public string? SecondaryColor { get; set; }       // Accent color
    public Font? Font { get; set; }                   // Typography
}
```

**Font Configuration:**
```csharp
public class Font
{
    public string? Family { get; set; }               // Font family name
    public int? FontSizeDefault { get; set; }         // Base font size
    public int? FontSizeMedium { get; set; }          // Medium text size
    public int? FontSizeSmall { get; set; }           // Small text size
}
```

**Margin Settings:**
```csharp
public class Margin
{
    public double? Top { get; set; }                  // Top margin
    public double? Right { get; set; }                // Right margin
    public double? Bottom { get; set; }               // Bottom margin
    public double? Left { get; set; }                 // Left margin
}
```

### 5. Entry Model (`@/Users/divyanshu/walkover/GiddhTemplate/Models/RequestModel.cs:301-319`)

**Purpose:** Individual invoice line items

**Structure:**
```csharp
public class Entry
{
    public string? Date { get; set; }                 // Item date
    public string? AccountName { get; set; }          // Item name
    public Amount? Amount { get; set; }               // Unit price
    public Stock? Stock { get; set; }                 // Product details
    public Amount? TaxableValue { get; set; }         // Pre-tax amount
    public List<Tax>? Discounts { get; set; }        // Applied discounts
    public List<Tax>? Taxes { get; set; }            // Applied taxes
    public Amount? SubTotal { get; set; }             // Line subtotal
    public Amount? EntryTotal { get; set; }           // Line total
    public string? Description { get; set; }          // Item description
    public string? HsnNumber { get; set; }            // HSN/SAC code
}
```

**Stock Details:**
```csharp
public class Stock
{
    public string? Name { get; set; }                 // Product name
    public StockUnit? StockUnit { get; set; }         // Unit of measure
    public Variant? Variant { get; set; }             // Product variant
    public decimal? Quantity { get; set; }            // Quantity
    public Amount? Rate { get; set; }                 // Unit rate
    public string? Sku { get; set; }                  // SKU code
    public bool? TaxInclusive { get; set; }           // Tax inclusion
}
```

### 6. Tax Models

#### Tax Structure (`@/Users/divyanshu/walkover/GiddhTemplate/Models/RequestModel.cs:293-299`)
```csharp
public class Tax
{
    public string? AccountName { get; set; }          // Tax name
    public Amount? Amount { get; set; }               // Tax amount
    public double? TaxPercent { get; set; }           // Tax rate
    public string? AccountUniqueName { get; set; }    // Tax identifier
}
```

#### GST Tax Totals (`@/Users/divyanshu/walkover/GiddhTemplate/Models/RequestModel.cs:213-226`)
```csharp
public class GstTaxesTotal
{
    public string? TaxType { get; set; }              // CGST/SGST/IGST
    public string? Name { get; set; }                 // Tax display name
    public Amount? Amount { get; set; }               // Tax amount
    public double? TaxPercent { get; set; }           // Tax percentage
    public bool? ConsiderInItemTotal { get; set; }    // Include in item total
    public bool? ShowOnVoucher { get; set; }          // Display on invoice
}
```

#### Tax Bifurcation (`@/Users/divyanshu/walkover/GiddhTemplate/Models/RequestModel.cs:228-252`)
```csharp
public class TaxBifurcation
{
    public decimal? TaxableValue { get; set; }        // Taxable amount
    public decimal? GstOrVatTaxRate { get; set; }     // GST/VAT rate
    public decimal? Iamt { get; set; }                // IGST amount
    public decimal? Camt { get; set; }                // CGST amount
    public decimal? Samt { get; set; }                // SGST amount
    public decimal? UtgstAmount { get; set; }         // UTGST amount
    public string? HsnSc { get; set; }                // HSN/SAC code
    public decimal? CessAmount { get; set; }          // Cess amount
}
```

### 7. Currency and Amount Models

#### Currency (`@/Users/divyanshu/walkover/GiddhTemplate/Models/RequestModel.cs:122-126`)
```csharp
public class Currency
{
    public string? Code { get; set; }                 // Currency code (USD, INR)
    public string? Symbol { get; set; }               // Currency symbol ($, ₹)
}
```

#### Amount (`@/Users/divyanshu/walkover/GiddhTemplate/Models/RequestModel.cs:196-200`)
```csharp
public class Amount
{
    public double? AmountForAccount { get; set; }     // Account currency
    public double? AmountForCompany { get; set; }     // Company currency
}
```

#### Amount as String (`@/Users/divyanshu/walkover/GiddhTemplate/Models/RequestModel.cs:202-206`)
```csharp
public class AmountAsString
{
    public string? AmountForAccount { get; set; }     // Formatted account amount
    public string? AmountForCompany { get; set; }     // Formatted company amount
}
```

### 8. Address and Customer Models

#### Address Details (`@/Users/divyanshu/walkover/GiddhTemplate/Models/RequestModel.cs:134-140`)
```csharp
public class AddressDetails
{
    public string? StateCounty { get; set; }          // State/County
    public string? Country { get; set; }              // Country
    public string? Address { get; set; }              // Full address
    public string? TaxNumber { get; set; }            // Regional tax number
}
```

#### Customer Details (`@/Users/divyanshu/walkover/GiddhTemplate/Models/RequestModel.cs:183-188`)
```csharp
public class CustomerDetails
{
    public string? Name { get; set; }                 // Customer name
    public string? Email { get; set; }                // Customer email
    public string? ContactNumber { get; set; }        // Customer phone
}
```

### 9. E-Invoice and Compliance

#### E-Invoice Details (`@/Users/divyanshu/walkover/GiddhTemplate/Models/RequestModel.cs:115-120`)
```csharp
public class EInvoiceDetails
{
    public string? IrnNumber { get; set; }            // IRN number
    public ulong? AcknowledgementNumber { get; set; } // Acknowledgment number
    public string? AcknowledgementDate { get; set; }  // Acknowledgment date
}
```

#### Bank QR Details (`@/Users/divyanshu/walkover/GiddhTemplate/Models/RequestModel.cs:321-324`)
```csharp
public class BankQRDetails
{
    public string? BankQRCodeBase64 { get; set; }     // QR code for payments
}
```

## Enums

### VoucherTypeEnums (`@/Users/divyanshu/walkover/GiddhTemplate/Models/Enums/VoucherTypeEnums.cs`)
```csharp
public enum VoucherTypeEnums
{
    Invoice,
    Estimate,
    ProformaInvoice,
    PurchaseOrder,
    Receipt,
    Payment,
    CreditNote,
    DebitNote
}
```

## Model Usage Patterns

### JSON Deserialization
```csharp
Root request = JsonSerializer.Deserialize<Root>(jsonString,
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
```

### Null Safety
- All properties are nullable (`string?`, `int?`, `bool?`)
- Null checks required before accessing nested properties
- Default values provided where appropriate

### Dynamic Settings
```csharp
// Initialize settings from dictionary
settings.InitializeStaticKeys();

// Check display preferences
if (settings.ShowLogo?.Display == true)
{
    // Render logo
}
```

### Multi-Currency Support
```csharp
// Handle different currencies
var accountAmount = entry.Amount?.AmountForAccount;
var companyAmount = entry.Amount?.AmountForCompany;
var currencySymbol = company.Currency?.Symbol ?? "$";
```

## Validation Considerations

### Required Fields
- `Company.Name` - Must not be null or empty
- `VoucherNumber` - Invoice identifier required
- `Entries` - At least one line item needed

### Data Integrity
- Tax calculations consistency
- Currency conversion accuracy
- Address completeness for compliance

### Business Rules
- GST compliance for Indian invoices
- E-invoice requirements for B2B transactions
- Tax bifurcation accuracy

---

**Author/Developer:** Divyanshu Shrivastava  
**Last Updated:** December 2025
