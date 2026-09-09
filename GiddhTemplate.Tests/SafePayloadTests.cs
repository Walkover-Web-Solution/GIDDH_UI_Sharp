using System.Linq;
using System.Text.Json;
using GiddhTemplate.Services;
using Xunit;

namespace GiddhTemplate.Tests
{
    public class SafePayloadTests
    {
        [Fact]
        public void Missing_key_is_not_null_and_does_not_throw()
        {
            dynamic model = SafePayload.From(Json("""{ "company": { "name": "Acme" } }"""));

            Assert.Equal("Acme", model.company.name.ToString());
            Assert.Equal(string.Empty, model.missing.ToString());
            Assert.Equal(string.Empty, model.account.name.ToString());
            Assert.True(SafeConvert.IsMissing(model.account));
            Assert.False(TemplateHelpers.HasKey(model, "account"));
            Assert.False(TemplateHelpers.Has(model, "account.name"));
            Assert.True(TemplateHelpers.Has(model, "company.name"));
        }

        [Fact]
        public void Decimal_cast_accepts_string_double_and_int()
        {
            dynamic model = SafePayload.From(Json("""{ "amount": "123.45", "rate": 18, "qty": 2.5 }"""));

            decimal amount = model.amount;
            decimal rate = model.rate;
            decimal qty = model.qty;

            Assert.Equal(123.45m, amount);
            Assert.Equal(18m, rate);
            Assert.Equal(2.5m, qty);
            Assert.Equal(123.45m, TemplateHelpers.Dec(model, "amount"));
            Assert.Equal(0m, TemplateHelpers.Dec(model, "unknown"));
        }

        [Fact]
        public void Boolean_comparison_accepts_string_and_number()
        {
            dynamic model = SafePayload.From(Json("""{ "flag": "true", "one": 1, "off": "no", "real": true }"""));

            Assert.True(model.flag == true);
            Assert.True(model.one == true);
            Assert.True(model.real == true);
            Assert.False(model.off == true);
            Assert.False(model.missing == true);
            Assert.True(TemplateHelpers.Bool(model, "flag"));
            Assert.False(TemplateHelpers.Bool(model, "off"));
        }

        [Fact]
        public void Foreach_and_count_skip_missing_or_wrong_type()
        {
            dynamic model = SafePayload.From(Json("""{ "entries": [ { "amount": "10" } ], "taxes": "not-a-list" }"""));

            Assert.Equal(1, TemplateHelpers.CountOf(model, "entries"));
            Assert.Equal(0, TemplateHelpers.CountOf(model, "taxes"));
            Assert.Equal(0, TemplateHelpers.CountOf(model, "absent"));
            Assert.Single(TemplateHelpers.Items(model, "entries"));
            Assert.Empty(TemplateHelpers.Items(model, "taxes"));
            Assert.Empty(TemplateHelpers.Items(model, "absent"));
        }

        [Fact]
        public void Arithmetic_with_mixed_types_does_not_throw()
        {
            dynamic model = SafePayload.From(Json("""{ "amount": "10", "tax": 2.5 }"""));

            decimal total = model.amount + model.tax + model.missing;
            Assert.Equal(12.5m, total);
            Assert.Equal(10m, (decimal)model.amount);
        }

        [Fact]
        public void String_equals_and_split_forward_to_raw_value()
        {
            dynamic model = SafePayload.From(Json("""{ "type": "proforma", "ids": "a, b, c" }"""));

            Assert.True(model.type.Equals("proforma") == true);
            object boxed = model;
            Assert.Equal(3, TemplateHelpers.Split(boxed, "ids").ToArray().Length);
            Assert.Empty(TemplateHelpers.Split(boxed, "missing").ToArray());
        }

        [Fact]
        public void Indexer_on_empty_list_returns_missing()
        {
            dynamic model = SafePayload.From(Json("""{ "entries": [] }"""));

            Assert.Equal(string.Empty, model.entries[0].quantity.ToString());
            Assert.Equal(0m, TemplateHelpers.Dec(model.entries[0], "quantity"));
        }

        [Fact]
        public void ShowCol_uses_defaults_when_table_labels_are_omitted()
        {
            object noTableLabels = SafePayload.From(Json("""{ "label": { "gstin": "GSTIN" } }"""));
            object withTableLabels = SafePayload.From(Json("""{ "label": { "item": "Item", "gstin": "GSTIN" } }"""));

            Assert.True(TemplateHelpers.ShowCol(noTableLabels, "sNo"));
            Assert.True(TemplateHelpers.ShowCol(noTableLabels, "item"));
            Assert.True(TemplateHelpers.ShowCol(noTableLabels, "quantity"));
            Assert.Equal("S No.", TemplateHelpers.ColLbl(noTableLabels, "sNo", "S No."));

            Assert.True(TemplateHelpers.ShowCol(withTableLabels, "item"));
            Assert.False(TemplateHelpers.ShowCol(withTableLabels, "sNo"));
        }

        [Fact]
        public void Items_walks_nested_entry_groups()
        {
            object model = SafePayload.From(Json("""
                { "entries": [ [ { "transactions": [ { "description": "Product A" } ] } ] ] }
                """));

            var groups = TemplateHelpers.Items(model, "entries").Cast<object>().ToList();
            Assert.Single(groups);

            var entries = TemplateHelpers.Items(groups[0]).Cast<object>().ToList();
            Assert.Single(entries);
            Assert.True(TemplateHelpers.Has(entries[0], "transactions"));
            Assert.Equal("Product A", TemplateHelpers.Str(entries[0], "transactions.0.description"));
        }

        private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();
    }
}
