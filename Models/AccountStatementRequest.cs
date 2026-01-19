using System.Text.Json.Serialization;

namespace GiddhTemplate.Models
{
    public class Root
    {
        public string TemplateType { get; set; } = string.Empty;
        public string FromDate { get; set; } = string.Empty;
        public string ToDate { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public Address? AccountAddress { get; set; }
        public Address? CompanyGstAddress { get; set; }

        public AccountSummaryInfo? AccountSummary { get; set; }
        public Balance? ClosingBalance { get; set; }
        public string AccountUniqueName { get; set; } = string.Empty;
        public List<TransactionDetail> TransactionDetailList { get; set; } = new List<TransactionDetail>();
        public decimal DebitTotal { get; set; } = 0;
        public decimal CreditTotal { get; set; } = 0;
        [JsonIgnore]
        public Balance? OpeningBalance { get; set; }
        public int Page { get; set; }
        public long Count { get; set; }
        public int TotalPages { get; set; }
        public long TotalItems { get; set; }
        [JsonIgnore]
        public long? TotalEntryCount { get; set; }

        public class AccountSummaryInfo
        {
            public Balance OpeningBalance { get; set; } = new Balance();
            public decimal DebitTotal { get; set; } = 0;
            public decimal CreditTotal { get; set; } = 0;
            public Balance ClosingBalance { get; set; } = new Balance();

            public AccountSummaryInfo() { }

            public AccountSummaryInfo(decimal debitTotal, decimal creditTotal)
            {
                DebitTotal = debitTotal;
                CreditTotal = creditTotal;
            }
        }

        public class TransactionDetail
        {
            public string Date { get; set; } = string.Empty;
            public string VoucherType { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public Balance VoucherAmount { get; set; } = new Balance();
            public Balance ClosingBalance { get; set; } = new Balance();
            public string? EntryUniqueName { get; set; }
            public string? VoucherNumber { get; set; }
            public Particular? Particular { get; set; }

            public TransactionDetail() { }

            public TransactionDetail(string date, string voucherType, string description, 
                Balance voucherAmount, Balance closingBalance, string? entryUniqueName, 
                string? voucherNumber, Particular? particular)
            {
                Date = date;
                VoucherType = voucherType;
                Description = description;
                VoucherAmount = voucherAmount;
                ClosingBalance = closingBalance;
                EntryUniqueName = entryUniqueName;
                VoucherNumber = voucherNumber;
                Particular = particular;
            }
        }

        public class Address
        {
            public string? TaxType { get; set; }
            public string? CountryName { get; set; }
            public string? CountryCode { get; set; }
            public string? Email { get; set; }
            public string? MobileNo { get; set; }
            public string? TaxNumber { get; set; }
            public string? StateName { get; set; }
            public string? StateCode { get; set; }
            public County? County { get; set; }
            public string? AttentionTo { get; set; }
            [JsonPropertyName("address")]
            public string? AddressLine { get; set; }
            public string? PinCode { get; set; }
            public CodeSymbolModel? Currency { get; set; }

            public Address() { }

            public Address(string? taxType, string? countryName, string? countryCode, 
                string? email, string? mobileNo, string? taxNumber, string? stateName, 
                string? stateCode, County? county, string? attentionTo, string? addressLine, 
                string? pinCode, CodeSymbolModel? currency)
            {
                TaxType = taxType;
                CountryName = countryName;
                CountryCode = countryCode;
                Email = email;
                MobileNo = mobileNo;
                TaxNumber = taxNumber;
                StateName = stateName;
                StateCode = stateCode;
                County = county;
                AttentionTo = attentionTo;
                AddressLine = addressLine;
                PinCode = pinCode;
                Currency = currency;
            }
        }
    }

    public class Balance
    {
        public decimal Amount { get; set; } = 0;
        public string Type { get; set; } = string.Empty;
        public string? Description { get; set; }

        public Balance() { }

        public Balance(decimal amount, string type)
        {
            Amount = amount;
            Type = type;
        }

        public Balance(decimal amount, string type, string? description)
        {
            Amount = amount;
            Type = type;
            Description = description;
        }
    }

    public class CodeSymbolModel
    {
        public string Code { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;

        public CodeSymbolModel() { }

        public CodeSymbolModel(string code, string symbol)
        {
            Code = code;
            Symbol = symbol;
        }
    }

    public class County
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;

        public County() { }

        public County(string name, string code)
        {
            Name = name;
            Code = code;
        }
    }

    public class Particular
    {
        public string Name { get; set; } = string.Empty;
        public string UniqueName { get; set; } = string.Empty;

        public Particular() { }

        public Particular(string name, string uniqueName)
        {
            Name = name;
            UniqueName = uniqueName;
        }
    }
}
