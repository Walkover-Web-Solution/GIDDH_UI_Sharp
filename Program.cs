using GiddhTemplate.Services;
using GiddhTemplate.Controllers;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Register services
        builder.Services.AddHttpClient();
        builder.Services.AddScoped<ISlackService, SlackService>();
        builder.Services.AddSingleton<PdfService>();
        builder.Services.AddScoped<AccountStatementPdfService>();

        builder.Services.AddControllers();
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(5000);
        });
        var app = builder.Build();
        await PdfService.GetBrowserAsync();
        app.MapControllers();
        await app.RunAsync();
    }
}
