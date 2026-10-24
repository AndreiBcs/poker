using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddSingleton<Arena>();
        builder.Services.AddSingleton<ArenaUpdates>();
        builder.Services.AddSignalR().AddJsonProtocol(options =>
            options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
        var app = builder.Build();
        app.Use(async (context, next) =>
        {
            try { await next(context); }
            catch (ArenaException ex) when (!context.Response.HasStarted)
            {
                context.Response.StatusCode = ex.Status;
                await context.Response.WriteAsJsonAsync(new { error = ex.Message });
            }
        });
        app.MapArenaEndpoints();
        app.Run();
    }
}
