// ErpPortal.Api/Program.cs
using ErpPortal.Api.Core.Config;
using ErpPortal.Api.Core.Contracts;
using ErpPortal.Api.Infrastructure.Http;
using ErpPortal.Api.Infrastructure.Services;
using Microsoft.Extensions.Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ─── Configuration Validation ─────────────────────────────────────────────────
builder.Services
    .AddOptions<DummyJsonSettings>()
    .BindConfiguration(DummyJsonSettings.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ─── Token Management (Singleton — shared across all requests) ────────────────
builder.Services.AddSingleton<ITokenService, TokenService>();

// ─── "Raw" HttpClient (no auth handler) — used by TokenService for login/refresh
builder.Services.AddHttpClient("DummyJsonRaw", (sp, client) =>
{
    DummyJsonSettings settings = sp.GetRequiredService<IOptions<DummyJsonSettings>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// ─── Typed HttpClient with Auth Handler (for protected /auth/* endpoints) ─────
builder.Services.AddTransient<DummyJsonAuthHandler>();

builder.Services
    .AddHttpClient<IDummyJsonClient, DummyJsonClient>((sp, client) =>
    {
        DummyJsonSettings settings = sp.GetRequiredService<IOptions<DummyJsonSettings>>().Value;
        client.BaseAddress = new Uri(settings.BaseUrl);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    })
    .AddHttpMessageHandler<DummyJsonAuthHandler>();

// ─── Controllers & Swagger ────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ─── Build ────────────────────────────────────────────────────────────────────
WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
