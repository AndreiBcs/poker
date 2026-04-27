using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapOpenApi();
app.MapScalarApiReference();

app.Run();
