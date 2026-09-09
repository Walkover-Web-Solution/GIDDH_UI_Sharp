using RazorLight;
using System.Text.Json;
using System.Text.RegularExpressions;
using GiddhTemplate.Services.GenericTemplate;

namespace GiddhTemplate.Services
{
    /// <summary>
    /// Razor renderer used only by <see cref="GenericPdfService"/> for dynamic
    /// <c>Templates/other-template</c> templates. Typed PDFs use <see cref="RazorTemplateService"/>.
    /// </summary>
    public class GenericRazorTemplateService
    {
        private const string OtherTemplateFolderSegment = "other-template";
        private const string TemplateCacheKeySuffix = "#generic-template-v3";

        private const string TemplateHelperImports =
            "@using System.Collections.Generic\n" +
            "@using System.Linq\n" +
            "@using static GiddhTemplate.Services.GenericTemplate.GenericTemplateHelpers\n";

        private static readonly Regex HasKeyFunctionStartPattern = new(
            @"\s*// Helper function to check if key exists in dynamic object\s*" +
            @"bool\s+HasKey\s*\(\s*dynamic\s+\w+\s*,\s*string\s+\w+\s*\)\s*\{",
            RegexOptions.Compiled);

        private static readonly Regex EmptyCodeBlockPattern = new(
            @"@using System\.Collections\.Generic;\s*@\{\s*\}\s*",
            RegexOptions.Compiled);

        private static readonly Regex StandaloneEmptyCodeBlockPattern = new(
            @"@\{\s*\}\s*",
            RegexOptions.Compiled);

        private readonly RazorLightEngine _engine;

        public GenericRazorTemplateService()
        {
            _engine = new RazorLightEngineBuilder()
                .UseEmbeddedResourcesProject(typeof(GenericRazorTemplateService))
                .UseFileSystemProject(Directory.GetCurrentDirectory())
                .UseMemoryCachingProvider()
                .Build();
        }

        public async Task<string> RenderTemplateAsync<T>(string templatePath, T model)
        {
            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Template file not found: {templatePath}");
            }

            if (!IsOtherTemplatePath(templatePath))
            {
                throw new InvalidOperationException(
                    $"Generic PDF safety logic only applies to templates under '{OtherTemplateFolderSegment}'. Path: {templatePath}");
            }

            string templateContent = PrepareTemplateContent(await File.ReadAllTextAsync(templatePath));

            try
            {
                object modelToRender = GenericJsonModelConverter.Normalize(model);

                return await _engine.CompileRenderStringAsync(
                    templatePath + TemplateCacheKeySuffix,
                    TemplateHelperImports + templateContent,
                    modelToRender
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GenericRazorTemplateService] Error rendering template {templatePath}: {ex.Message}");
                throw;
            }
        }

        internal static bool IsOtherTemplatePath(string templatePath) =>
            templatePath.Replace('\\', '/').Contains($"/{OtherTemplateFolderSegment}/", StringComparison.OrdinalIgnoreCase);

        internal static string PrepareTemplateContent(string templateContent)
        {
            var prepared = RemoveHasKeyFunction(templateContent);
            prepared = EmptyCodeBlockPattern.Replace(prepared, string.Empty);
            prepared = StandaloneEmptyCodeBlockPattern.Replace(prepared, string.Empty);
            return prepared.TrimStart();
        }

        private static string RemoveHasKeyFunction(string templateContent)
        {
            while (true)
            {
                var match = HasKeyFunctionStartPattern.Match(templateContent);
                if (!match.Success)
                {
                    break;
                }

                var functionBodyStart = match.Index + match.Length - 1;
                var functionBodyEnd = FindMatchingBrace(templateContent, functionBodyStart);
                if (functionBodyEnd < 0)
                {
                    break;
                }

                templateContent = templateContent.Remove(match.Index, functionBodyEnd - match.Index + 1);
            }

            return templateContent;
        }

        private static int FindMatchingBrace(string content, int openBraceIndex)
        {
            var depth = 0;

            for (var i = openBraceIndex; i < content.Length; i++)
            {
                if (content[i] == '{')
                {
                    depth++;
                }
                else if (content[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }
    }
}
