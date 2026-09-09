using RazorLight;

namespace GiddhTemplate.Services
{
    public class GenericRazorTemplateService
    {
        private readonly RazorLightEngine _engine;

        // Imported into every generic template so a template can use the safe helpers
        // (Has / Get / Str / Dec / Int / Bool / Items ...) without declaring anything itself.
        private static readonly string[] TemplateUsings =
        {
            "@using System",
            "@using System.Linq",
            "@using System.Collections.Generic",
            "@using GiddhTemplate.Services",
            "@using static GiddhTemplate.Services.TemplateHelpers"
        };

        public GenericRazorTemplateService()
        {
            _engine = new RazorLightEngineBuilder()
                .UseEmbeddedResourcesProject(typeof(GenericRazorTemplateService))
                .UseFileSystemProject(Directory.GetCurrentDirectory())
                .UseMemoryCachingProvider()
                .Build();
        }

        private const string CacheVersion = "safe-v2";

        public async Task<string> RenderTemplateAsync<T>(string templatePath, T model)
        {
            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Template file not found: {templatePath}");
            }

            string templateContent = await File.ReadAllTextAsync(templatePath);

            try
            {
                // Payloads are schema-less: render against a model where a missing key or an
                // unexpected type resolves to a neutral value instead of throwing.
                object safeModel = SafePayload.From(model);

                return await _engine.CompileRenderStringAsync(
                    $"{templatePath}::{CacheVersion}",
                    AddTemplateUsings(templateContent),
                    safeModel
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GenericRazorTemplateService] Error rendering template {templatePath}: {ex.Message}");
                throw;
            }
        }

        private static string AddTemplateUsings(string templateContent)
        {
            var preamble = string.Join(
                Environment.NewLine,
                TemplateUsings.Where(directive => !templateContent.Contains(directive, StringComparison.Ordinal)));

            return string.IsNullOrEmpty(preamble)
                ? templateContent
                : $"{preamble}{Environment.NewLine}{templateContent}";
        }
    }
}
