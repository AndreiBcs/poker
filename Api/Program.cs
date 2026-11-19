using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Contracts;
using Api.Endpoints;
using Api.Hubs;
using Api.Services;
using Microsoft.Extensions.Logging.EventLog;

namespace Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        if (OperatingSystem.IsWindows())
        {
            // The default Event Log source may be inaccessible to a local developer account.
            builder.Logging.AddFilter<EventLogLoggerProvider>(null, LogLevel.None);
        }

        builder.Services.AddSingleton<Arena>();
        builder.Services.AddSingleton<ArenaUpdates>();
        builder.Services.AddSignalR().AddJsonProtocol(options =>
            options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
        var app = builder.Build();
        app.Use(async (context, next) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            try
            {
                var request = context.Request;
                if ((HttpMethods.IsPost(request.Method) || HttpMethods.IsDelete(request.Method) ||
                     request.Path.StartsWithSegments("/api/live")) &&
                    request.Headers.TryGetValue("Origin", out var origin) &&
                    !string.Equals(origin.ToString(), $"{request.Scheme}://{request.Host}", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArenaException(403, "Use the Poker site to access this game.");
                }
                await next(context);
            }
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
