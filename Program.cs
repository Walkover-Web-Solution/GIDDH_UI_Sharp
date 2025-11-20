using GiddhTemplate.Services;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddHttpClient();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ISlackService, SlackService>();
        builder.Services.AddScoped<PdfService>();
        builder.Services.AddControllers();

        var app = builder.Build();
        app.MapControllers();
        await app.RunAsync();
    }
}
