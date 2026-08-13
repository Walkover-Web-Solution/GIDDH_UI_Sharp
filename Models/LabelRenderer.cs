using System.Net;
using System.Text;
using RazorLight.Text;

namespace InvoiceData
{
    /// <summary>
    /// Direction in which the two label languages are stacked.
    /// </summary>
    public enum LabelLayout
    {
        /// <summary>Side by side (left to right / right to left when reversed). Used for normal labels.</summary>
        Inline = 0,

        /// <summary>One below the other (top to bottom / bottom to top when reversed). Used for table headers.</summary>
        Stacked = 1
    }

    /// <summary>
    /// Single place that turns a <see cref="Setting"/> into printable label markup honouring
    /// <see cref="LabelDisplayConfig"/>. Shared by every Razor template so all of them behave identically.
    /// </summary>
    public static class LabelRenderer
    {
        private const string DefaultSeparator = " ";

        private static readonly HashSet<string> RightToLeftLanguages = new(StringComparer.OrdinalIgnoreCase)
        {
            "ar", "arc", "ckb", "dv", "fa", "ha", "he", "iw", "khw", "ks", "ku", "ps", "sd", "syr", "ug", "ur", "yi"
        };

        /// <summary>Plain text label. Use it for comparisons, emptiness checks and string interpolation.</summary>
        public static string Text(Setting? setting, LabelDisplayConfig? config)
        {
            var parts = Resolve(setting, config);
            if (parts.Count == 0)
            {
                return string.Empty;
            }

            if (parts.Count == 1)
            {
                return parts[0].Value;
            }

            if (config?.SecondaryLabelFirst == true)
            {
                return string.Concat(parts[1].Value, Separator(config), parts[0].Value);
            }

            return string.Concat(parts[0].Value, Separator(config), parts[1].Value);
        }

        /// <summary>
        /// Label markup. When a single language is printed the output is plain text, i.e. byte for byte
        /// identical to the previous single language behaviour. Only when both languages are printed the
        /// inline-flex wrapper is emitted, with the two languages placed in the markup in their actual
        /// display order (rather than flipped through CSS flex-direction, which breaks once the label
        /// wraps onto a second line).
        /// </summary>
        public static IRawString Html(Setting? setting, LabelDisplayConfig? config, LabelLayout layout = LabelLayout.Inline)
        {
            var parts = Resolve(setting, config);
            if (parts.Count == 0)
            {
                return new RawString(string.Empty);
            }

            if (parts.Count == 1)
            {
                var only = parts[0];
                return new RawString(IsRightToLeft(only.Language)
                    ? string.Concat("<span", LanguageAttributes(only.Language), ">", Encode(only.Value), "</span>")
                    : Encode(only.Value));
            }

            var first = parts[0];
            var second = parts[1];
            if (config?.SecondaryLabelFirst == true)
            {
                first = parts[1];
                second = parts[0];
            }

            var builder = new StringBuilder();
            builder.Append("<span class=\"lbl-multi");
            builder.Append(layout == LabelLayout.Stacked ? " lbl-stack" : " lbl-inline");
            builder.Append("\">");

            builder.Append("<span class=\"lbl-1\"").Append(LanguageAttributes(first.Language)).Append('>')
                   .Append(Encode(first.Value)).Append("</span>");

            if (layout == LabelLayout.Inline)
            {
                builder.Append("<span class=\"lbl-sep\">").Append(Encode(Separator(config))).Append("</span>");
            }

            builder.Append("<span class=\"lbl-2\"").Append(LanguageAttributes(second.Language)).Append('>')
                   .Append(Encode(second.Value)).Append("</span>");

            builder.Append("</span>");
            return new RawString(builder.ToString());
        }

        /// <summary>
        /// Plain text of a value that is sent in both languages, e.g. Message1 / SecondaryMessage1.
        /// Use it for emptiness checks before emitting the surrounding markup.
        /// </summary>
        public static string Text(string? primary, string? secondary, LabelDisplayConfig? config)
        {
            return Text(Pair(primary, secondary), config);
        }

        /// <summary>
        /// Markup for a value that is sent in both languages, e.g. Message1 / SecondaryMessage1. Each language
        /// gets its own block element because such a value can span multiple lines, which the inline-flex
        /// wrapper built by <see cref="Html"/> does not handle. Languages are emitted in display order.
        /// </summary>
        public static IRawString HtmlBlocks(string? primary, string? secondary, LabelDisplayConfig? config, string tag = "p", string? cssClass = null)
        {
            var parts = Ordered(Pair(primary, secondary), config);
            if (parts.Count == 0)
            {
                return new RawString(string.Empty);
            }

            var classAttribute = string.IsNullOrWhiteSpace(cssClass) ? string.Empty : $" class=\"{Encode(cssClass)}\"";
            var builder = new StringBuilder();
            foreach (var part in parts)
            {
                builder.Append('<').Append(tag).Append(classAttribute).Append(LanguageAttributes(part.Language)).Append('>')
                       .Append(Encode(part.Value))
                       .Append("</").Append(tag).Append('>');
            }

            return new RawString(builder.ToString());
        }

        /// <summary>True when the given language code is written right to left.</summary>
        public static bool IsRightToLeft(string? language)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                return false;
            }

            var primary = language.Split('-', '_')[0].Trim();
            return RightToLeftLanguages.Contains(primary);
        }

        /// <summary>
        /// Builds the list of languages that must be printed, primary first and secondary second.
        /// Callers (<see cref="Text"/> and <see cref="Html"/>) reorder these two parts for display when
        /// <see cref="LabelDisplayConfig.SecondaryLabelFirst"/> is set. Never returns an empty label when
        /// at least one of the two languages has text: the other language is used as a fallback.
        /// </summary>
        private static List<LabelPart> Resolve(Setting? setting, LabelDisplayConfig? config)
        {
            var parts = new List<LabelPart>(2);
            if (setting == null)
            {
                return parts;
            }

            var primary = (setting.Label ?? string.Empty).Trim();
            var secondary = (setting.SecondaryLabel ?? string.Empty).Trim();
            var hasPrimary = !string.IsNullOrWhiteSpace(primary);
            var hasSecondary = !string.IsNullOrWhiteSpace(secondary);

            var showPrimary = config?.ShowLabel ?? true;
            var showSecondary = config?.ShowSecondaryLabel ?? false;

            // Nothing selected: behave like the default single language setup.
            if (!showPrimary && !showSecondary)
            {
                showPrimary = true;
            }

            // Fall back to the other language instead of printing an empty label.
            if (showPrimary && !showSecondary && !hasPrimary && hasSecondary)
            {
                showPrimary = false;
                showSecondary = true;
            }
            else if (showSecondary && !showPrimary && !hasSecondary && hasPrimary)
            {
                showSecondary = false;
                showPrimary = true;
            }

            // Markup always keeps the primary language first, the visual flip is done by flex-direction.
            if (showPrimary && hasPrimary)
            {
                parts.Add(new LabelPart(primary, config?.LabelLanguage));
            }

            if (showSecondary && hasSecondary)
            {
                parts.Add(new LabelPart(secondary, config?.SecondaryLabelLanguage));
            }

            // Identical text in both languages: print once, do not duplicate.
            if (parts.Count == 2 && string.Equals(parts[0].Value, parts[1].Value, StringComparison.Ordinal))
            {
                parts.RemoveAt(1);
            }

            return parts;
        }

        /// <summary>Same as <see cref="Resolve"/>, but already reordered into the actual display order.</summary>
        private static List<LabelPart> Ordered(Setting? setting, LabelDisplayConfig? config)
        {
            var parts = Resolve(setting, config);
            if (parts.Count == 2 && config?.SecondaryLabelFirst == true)
            {
                return new List<LabelPart> { parts[1], parts[0] };
            }

            return parts;
        }

        /// <summary>Wraps two raw strings so they can go through the same resolution as a setting's labels.</summary>
        private static Setting Pair(string? primary, string? secondary)
        {
            return new Setting { Label = primary, SecondaryLabel = secondary };
        }

        private static string Separator(LabelDisplayConfig? config)
        {
            return string.IsNullOrEmpty(config?.Separator) ? DefaultSeparator : config!.Separator!;
        }

        private static string LanguageAttributes(string? language)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                return string.Empty;
            }

            var encoded = Encode(language);
            return IsRightToLeft(language)
                ? $" lang=\"{encoded}\" dir=\"rtl\""
                : $" lang=\"{encoded}\" dir=\"ltr\"";
        }

        private static string Encode(string? value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : WebUtility.HtmlEncode(value);
        }

        private sealed class LabelPart
        {
            public LabelPart(string value, string? language)
            {
                Value = value;
                Language = language;
            }

            public string Value { get; }
            public string? Language { get; }
        }
    }
}
