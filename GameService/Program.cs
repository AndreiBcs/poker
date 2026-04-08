
using GameService.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // This dynamically accepts your Live Server (or any local port) 
        // without using the forbidden wildcard '*'
        policy.SetIsOriginAllowed(origin => true) 
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // SignalR strictly requires this!
    });
});

builder.Services.AddOpenApi();

builder.Services.AddSignalR(); 
var app = builder.Build();
app.UseCors();

app.MapHub<GameHub>("/gamehub");

app.Run();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapHub<GameHub>("/gamehub");

app.Run();

