using Microsoft.EntityFrameworkCore;
using Source.Features.Game;
using Source.Features.Lobby;
using Source.Features.User;
using Source.Infrastructure.Authentication;
using Source.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddCookieAuthentication();
builder.Services.AddScoped<UserServices>();
builder.Services.AddScoped<LobbyService>();
builder.Services.AddScoped<UserSessionService>();
builder.Services.AddSingleton<GameStateStore>();
builder.Services.AddSingleton<GameConnectionManager>();
builder.Services.AddScoped<GameService>();
builder.Services.AddScoped<GameWebSocketHandler>();
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "app.db");
var connectionString = $"Data Source={dbPath}";

builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(connectionString));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseWebSockets();

app.UseAuthentication();
app.UseAuthorization();

app.MapUserEndpoints();
app.MapLobbyEndpoints();
app.MapGameEndpoints();
app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
