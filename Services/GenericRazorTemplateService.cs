using RazorLight;
using System.Text.Json;

namespace GiddhTemplate.Services
{
    public class GenericRazorTemplateService
    {
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

            string templateContent = await File.ReadAllTextAsync(templatePath);

            try
            {
                // For JsonElement, convert to a dynamic-compatible object
                object modelToRender = model;
                if (model is JsonElement jsonElement)
                {
                    modelToRender = ConvertJsonElementToDynamic(jsonElement);
                }

                return await _engine.CompileRenderStringAsync(
                    templatePath,
                    templateContent,
                    modelToRender
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GenericRazorTemplateService] Error rendering template {templatePath}: {ex.Message}");
                throw;
            }
        }

        private dynamic ConvertJsonElementToDynamic(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Object => ConvertJsonElementToExpandoObject(element),
                JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElementToDynamic).ToList(),
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt32(out var intVal) ? intVal : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => element.GetRawText()
            };
        }

        private dynamic ConvertJsonElementToExpandoObject(JsonElement element)
        {
            var expando = new System.Dynamic.ExpandoObject();
            var dict = (IDictionary<string, object>)expando;

            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    // Recursively convert nested objects to ExpandoObject
                    object value = property.Value.ValueKind == JsonValueKind.Object
                        ? ConvertJsonElementToExpandoObject(property.Value)
                        : ConvertJsonElementToDynamic(property.Value);
                    
                    dict[property.Name] = value;
                }
            }

            return expando;
        }
    }
}
