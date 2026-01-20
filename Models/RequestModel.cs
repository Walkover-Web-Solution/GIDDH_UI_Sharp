using GiddhTemplate.Models.Enums;
namespace InvoiceData
{
    public class Settings
    {
        public Dictionary<string, Setting>? SettingDetails { get; set; }
        public Setting? ShowLogo { get; set; }
        public Setting? TaxableAmount { get; set; }
        public Setting? ShowNotesAtLastPage { get; set; }
        public Setting? Tds { get; set; }
        public Setting? ImageSignature { get; set; }
        public Setting? ShowMessage2 { get; set; }
        public Setting? Tcs { get; set; }
        public Setting? GrandTotal { get; set; }
        public Setting? TotalDue { get; set; }
        public Setting? HeaderCompanyName { get; set; }
        public Setting? FooterCompanyName { get; set; }
        public Setting? DisplayExportMessage { get; set; }
        public Setting? GrandTotalInAccountsCurrency { get; set; }
        public Setting? TotalInWordsInAccountsCurrency { get; set; }
        public Setting? TotalTax { get; set; }
        public Setting? Thanks { get; set; }
        public Setting? TaxBifurcation { get; set; }
        public Setting? TotalInWords { get; set; }
        public Setting? CompanyAddress { get; set; }
        public Setting? TextUnderSlogan { get; set; }
        public Setting? Slogan { get; set; }
        public Setting? Message1 { get; set; }
        public Setting? WarehouseAddress { get; set; }
        public Setting? ShippingDate { get; set; }
        public Setting? CustomField1 { get; set; }
        public Setting? CustomField2 { get; set; }
        public Setting? CustomField3 { get; set; }
        public Setting? ShippedVia { get; set; }
        public Setting? DueDate { get; set; }
        public Setting? DisplayExchangeRate { get; set; }
        public Setting? DisplayPlaceOfSupply { get; set; }
        public Setting? DisplayPlaceOfCountry { get; set; }
        public Setting? ShippingTaxNumber { get; set; }
        public Setting? CompanyTaxNumber { get; set; }
        public Setting? VoucherNumber { get; set; }
        public Setting? CustomerEmail { get; set; }
        public Setting? ShowQrCode { get; set; }
        public Setting? VoucherDate { get; set; }
        public Setting? CustomerContactNumber { get; set; }
        public Setting? AttentionTo { get; set; }
        public Setting? DisplayLutNumber { get; set; }
        public Setting? ShowCompanyAddress { get; set; }
        public Setting? Pan { get; set; }
        public Setting? TrackingNumber { get; set; }
        public Setting? ShippingCounty { get; set; }
        public Setting? FormNameInvoice { get; set; }
        public Setting? BillingTaxNumber { get; set; }
        public Setting? ShowEInvoiceDetails { get; set; }
        public Setting? Address { get; set; }
        public Setting? BillingStateCounty { get; set; }
        public Setting? CustomerName { get; set; }
        public Setting? FormNameTaxInvoice { get; set; }
        public Setting? ShippingAddress { get; set; }
        public Setting? ShipTo { get; set; }
        public Setting? ShippingStateCounty { get; set; }
        public Setting? BillingAddress { get; set; }
        public Setting? BillTo { get; set; }
        public Setting? BillingCounty { get; set; }
        public Setting? GstComposition { get; set; }
        public Setting? DisplayBaseCurrency { get; set; }
        public Setting? OtherTaxBifurcation { get; set; }
        public Setting? SNo { get; set; }
        public Setting? Item { get; set; }
        public Setting? Description { get; set; }
        public Setting? ShowDescriptionInRows { get; set; }
        public Setting? Date { get; set; }
        public Setting? HsnSac { get; set; }
        public Setting? Quantity { get; set; }
        public Setting? Discount { get; set; }
        public Setting? Taxes { get; set; }
        public Setting? Rate { get; set; }
        public Setting? Total { get; set; }
        public Setting? TotalQuantity { get; set; }
        public Setting? ShowVariantImage { get; set; }
        public Setting? TaxableValue { get; set; }
        public Setting? PreviousDue { get; set; }
        public Setting? AmountBeforeDiscount { get; set; }
        public Setting? SkuCode { get; set; }
        public Setting? GstSchemeData { get; set; }

        // Initialize all static keys dynamically
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

        // Helper method to retrieve settings from the dictionary
        private Setting? GetSetting(string key)
        {
            return SettingDetails != null && SettingDetails.ContainsKey(key)
                ? SettingDetails[key]
                : null;
        }
    }

    public class Setting
    {
        public string? Label { get; set; } = string.Empty;
        public bool? Display { get; set; }
    }

    public class LabelValue
    {
        public string? Label { get; set; } = string.Empty;
        public object? Value { get; set; } = null;
    }

    public class EInvoiceDetails
    {
        public string? IrnNumber { get; set; } = string.Empty;
        public ulong? AcknowledgementNumber { get; set; }
        public string? AcknowledgementDate { get; set; } = string.Empty;
    }

    public class Currency
    {
        public string? Code { get; set; }
        public string? Symbol { get; set; }
    }

    public class Logo
    {
        public string? Url { get; set; }
        public string? Size { get; set; }
    }

    public class AddressDetails
    {
        public string? StateCounty { get; set; }
        public string? Country { get; set; }
        public string? Address { get; set; }
        public string? TaxNumber { get; set; }
        public string? PinCode { get; set; }
    }

    public class Company
    {
        public string? Name { get; set; }
        public string? DefaultAddress { get; set; }
        public string? Address { get; set; }
        public string? ContactNumber { get; set; }
        public string? Email { get; set; }
        public Currency? Currency { get; set; }
        public AddressDetails? Billing { get; set; }
        public AddressDetails? Shipping { get; set; }
        public Logo? Logo { get; set; }
        public string? TaxNumber { get; set; }
        public string? HeaderCompanyName { get; set; }
        public string? GstSchemeData { get; set; }
    }

    public class Theme
    {
        public bool? MarginEnable { get; set; }
        public Margin? Margin { get; set; }
        public string? PrimaryColor { get; set; }
        public string? SecondaryColor { get; set; }
        public Font? Font { get; set; }
    }

    public class Margin
    {
        public double? Top { get; set; }
        public double? Right { get; set; }
        public double? Bottom { get; set; }
        public double? Left { get; set; }
    }

    public class Font
    {
        public string? Family { get; set; }
        public int? FontSizeDefault { get; set; }
        public int? FontSizeMedium { get; set; }
        public int? FontSizeSmall { get; set; }
    }

    public class CustomerDetails
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? ContactNumber { get; set; }
    }

    public class WarehouseDetails
    {
        public string? Name { get; set; }
        public string? Address { get; set; }
    }

    public class Amount
    {
        public double? AmountForAccount { get; set; }
        public double? AmountForCompany { get; set; }
    }

    public class AmountAsString
    {
        public string? AmountForAccount { get; set; }
        public string? AmountForCompany { get; set; }
    }

    public class SumOfTax
    {
        public Amount? Amount { get; set; }
    }

    public class GstTaxesTotal
    {
        public string? TaxType { get; set; }
        public string? Name { get; set; } // In Case of Estimate/Proforma
        public string? AccountName { get; set; }
        public Amount? Amount { get; set; }
        public double? TaxPercent { get; set; }
        public bool? ConsiderInItemTotal { get; set; }
        public bool? ConsiderInVoucherTotal { get; set; }
        public bool? ShowOnVoucher { get; set; }
        public string? AmountForAccount { get; set; }
        public string? AccountUniqueName { get; set; }
        public string? Key { get; set; }
    }

    public class TaxBifurcation
    {
        public decimal? Qty { get; set; }
        public decimal? EntryTotal { get; set; }
        public decimal? EntryTaxTotal { get; set; }
        public string? Desc { get; set; }
        public decimal? Iamt { get; set; }
        public decimal? Camt { get; set; }
        public decimal? Samt { get; set; }
        public decimal? UtgstAmount { get; set; }
        public decimal? TaxableValue { get; set; }
        public decimal? GstOrVatTaxRate { get; set; }
        public decimal? CsTaxRate { get; set; }
        public string? HsnSc { get; set; }
        public string? CessUniqueName { get; set; }
        public decimal? Vatamt { get; set; }
        public decimal? SalesTaxAmount { get; set; }
        public decimal? SalesTaxRate { get; set; }
        public decimal? OthTaxRate { get; set; }
        public decimal? CessAmount { get; set; }
        public decimal? OtherAmount { get; set; }
        public List<decimal>? CsAmountList { get; set; } = new List<decimal>();
        public List<decimal>? OthAmountList { get; set; } = new List<decimal>();
        public bool? HsnStatus { get; set; }
    }

    public class StockUnit
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
    }

    public class Variant
    {
        public string? Name { get; set; }
        public string? ImageBase64 { get; set; }
        public string? Value { get; set; }
    }

    public class Stock
    {
        public string? Name { get; set; }
        public StockUnit? StockUnit { get; set; }
        public Variant? Variant { get; set; }
        public Dictionary<string, string>? CustomFields { get; set; }
        public decimal? Quantity { get; set; }
        public Amount? Rate { get; set; }
        public string? Type { get; set; }
        public string? Sku { get; set; }
        public bool? TaxInclusive { get; set; }
        public bool? ShowVariant { get; set; }
        public bool? HasVariants { get; set; }
        public List<Amount>? UnitRates { get; set; }
        public StockCustomField? CustomField1 { get; set; }
        public StockCustomField? CustomField2 { get; set; }
        public string? SkuCodeHeading { get; set; }

    }

    public class StockCustomField
    {
        public string? Key { get; set; }
        public string? Value { get; set; }
    }

    public class Tax
    {
        public string? AccountName { get; set; }
        public Amount? Amount { get; set; }
        public double? TaxPercent { get; set; }
        public string? AccountUniqueName { get; set; }
    }

    public class Entry
    {
        public string? Date { get; set; }
        public string? AccountName { get; set; }
        public Amount? Amount { get; set; }
        public Stock? Stock { get; set; }
        public Amount? TaxableValue { get; set; }
        public List<Tax>? Discounts { get; set; }
        public List<Tax>? Taxes { get; set; }
        public Amount? SubTotal { get; set; }
        public Amount? TaxTotal { get; set; }
        public Amount? EntryTotal { get; set; }
        public Amount? GrandTotal { get; set; }
        public Amount? SumOfDiscounts { get; set; }
        public string? Description { get; set; }
        public string? HsnNumber { get; set; }
        public double? UsedQuantity { get; set; }
        public string? SacNumber { get; set; }
    }

    public class BankQRDetails
    {
        public string? BankQRCodeBase64 { get; set; }
    }

    public class CommonDiscountEntry
    {
        public string? AccountName { get; set; }
        public double? Amount { get; set; }
    }

    public class Root
    {
        public Settings? Settings { get; set; }
        public Company? Company { get; set; }
        public string? VoucherNumber { get; set; }
        public string? VoucherDate { get; set; }
        public AddressDetails? Billing { get; set; }
        public AddressDetails? Shipping { get; set; }
        public Theme? Theme { get; set; }
        public CustomerDetails? CustomerDetails { get; set; }
        public string? CustomField1 { get; set; }
        public string? CustomField2 { get; set; }
        public string? CustomField3 { get; set; }
        public string? ShippedVia { get; set; }
        public string? TcsTaxes { get; set; }
        public WarehouseDetails? WarehouseDetails { get; set; }
        public Amount? Balance { get; set; }
        public string? PlaceOfSupply { get; set; }
        public double? ExchangeRate { get; set; }
        public string? PlaceOfCountry { get; set; }
        public string? LutNumber { get; set; }
        public string? CustomerName { get; set; }
        public Amount? TaxableAmount { get; set; }
        public Amount? GrandTotal { get; set; }
        public string? QRCodeBase64String { get; set; }
        public List<SumOfTax>? SumOfTaxes { get; set; }
        public Amount? TcsTotalTax { get; set; }
        public Amount? PaidAmount { get; set; }
        public bool? IsBusinessToCustomerInvoice { get; set; }
        public bool? DisplayBaseCurrency { get; set; }
        public bool? ShowSectionsInline { get; set; }
        public Amount? TotalDue { get; set; }
        public Amount? PreviousDueAmount { get; set; }
        public string? GstComposition { get; set; }
        public string? Message2 { get; set; }
        public string? Message1 { get; set; }
        public string? StockQuantityWithUnit { get; set; }
        public string? DueDate { get; set; }
        public Currency? AccountCurrency { get; set; }
        public Currency? CompanyCurrency { get; set; }
        public Amount? TotalTax { get; set; }
        public Amount? TaxableTotal { get; set; }
        public AmountAsString? TotalInWords { get; set; }
        public Amount? GstEntriesTotal { get; set; }
        public Amount? TaxBifurcationEntryTotal { get; set; }
        public string? AttentionTo { get; set; }
        public string? TrackingNumber { get; set; }
        public string? CurrencyFormat { get; set; }
        public string? ImageSignature { get; set; } = string.Empty;
        public string? TemplateType { get; set; }

        public List<GstTaxesTotal>? GstTaxesTotal { get; set; }
        public List<GstTaxesTotal>? GstTaxesTotalOnly { get; set; }
        public List<GstTaxesTotal>? CessTotal { get; set; }
        public List<GstTaxesTotal>? OtherTotal { get; set; }

        public List<TaxBifurcation>? TaxBifurcation { get; set; }


        public bool? IsTaxesApplied { get; set; }
        public bool? IsBusinessToBusinessInvoice { get; set; }
        public bool? IsMultipleCurrency { get; set; }

        public List<Entry>? Entries { get; set; }
        public EInvoiceDetails? EInvoiceDetails { get; set; }
        public string? RoundOff { get; set; }
        public string? PdfRename { get; set; } = string.Empty; // This key used only in local pdf generation to Rename based on request
        public string? ShippingDate { get; set; }
        public string? GrandTotalInAccountsCurrency { get; set; }
        public string? TotalInWordsInAccountsCurrency { get; set; }
        public string? Currency { get; set; }
        public Amount? TdsTotalTax { get; set; }
        public string? TypeOfCopy { get; set; }
        public string? SealPath { get; set; }
        public bool? Reversecharge { get; set; }
        public string? ReverseChargeMessage { get; set; }
        public BankQRDetails? BankQRDetails { get; set; }
        public bool? ShowBankQr { get; set; }
        public string? formNameInvoice { get; set; }
        public string? CompanyTaxType { get; set; }
        public List<CommonDiscountEntry>? CommonDiscountEntries { get; set; }
        public double? OtherDiscount { get; set; }
        public string? VoucherType { get; set; }
        public VoucherTypeEnums VoucherTypeEnums { get; set; }
        public double? PaymentTotal { get; set; }
        public string? ChequeNumber { get; set; }
        public List<LabelValue>? AccountCustomFields { get; set; }
    }
}
