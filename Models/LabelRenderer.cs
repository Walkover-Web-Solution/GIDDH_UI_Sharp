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

            var primary = setting.Label ?? string.Empty;
            var secondary = setting.SecondaryLabel ?? string.Empty;
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

            return parts;
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
