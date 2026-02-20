using MedReminder.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DB
builder.Services.AddDbContext<CareHubDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CareHubDb")));

// Simple CORS for local MAUI dev later
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", p =>
        p.AllowAnyOrigin()
         .AllowAnyHeader()
         .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors("DevCors");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Minimal “is alive” endpoints for demo
app.MapGet("/health", () => Results.Ok(new { ok = true }));

app.MapControllers();

app.Run();