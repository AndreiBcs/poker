using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using UserService.Dependencies;
using UserService.Features.User;
using UserService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDatabase(builder.Configuration)
                .AddRepositories()
                .AddServices()
                .AddValidators()
                .AddMessaging(builder.Configuration)
                .AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapUserEndpoints();
app.Run();
