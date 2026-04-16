using ErpPortal.Core.Config;
using ErpPortal.Core.Contracts;
using ErpPortal.Infrastructure.Http;
using ErpPortal.Infrastructure.Repositories;
using ErpPortal.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using ErpPortal.Core.Domain;
using ErpPortal.Components;
using MudBlazor.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ─── Options & Configuration Validation (replaces Zod envSchema) ─────────────
builder.Services
    .AddOptions<ApiSettings>()
    .BindConfiguration(ApiSettings.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<BrandingConfig>()
    .BindConfiguration(BrandingConfig.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ─── Authentication (HTTP-only cookies — more secure than SPA localStorage) ───
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath    = "/login";
        options.LogoutPath   = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy   = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddControllersWithViews(); // Required for [ValidateAntiForgeryToken] filters

// ─── Output Caching (replaces TanStack Query's staleTime) ────────────────────
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(policy => policy.Expire(TimeSpan.FromMinutes(5)));
    options.AddPolicy("UsersList", policy =>
        policy.Expire(TimeSpan.FromMinutes(5)).Tag("users"));
    options.AddPolicy("TodosList", policy =>
        policy.Expire(TimeSpan.FromMinutes(5)).Tag("todos"));
});

// ─── HTTP Client (Typed Client with DelegatingHandlers = Axios interceptors) ──
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<AuthTokenHandler>();
builder.Services.AddTransient<ErrorHandlingHandler>();

builder.Services
    .AddHttpClient<IErpHttpClient, ErpHttpClient>((sp, client) =>
    {
        ApiSettings settings = sp.GetRequiredService<IOptions<ApiSettings>>().Value;
        client.BaseAddress = new Uri(settings.BaseUrl);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    })
    .AddHttpMessageHandler<AuthTokenHandler>()
    .AddHttpMessageHandler<ErrorHandlingHandler>();

// ─── Application Services (Scoped = per HTTP request, same as React's request scope) ─
builder.Services.AddScoped<IAuthService,         AuthService>();
builder.Services.AddScoped<INotificationService, MudBlazorNotificationService>();
builder.Services.AddScoped<IRepository<User>,    UserRepository>();
builder.Services.AddScoped<IRepository<Todo>,    TodoRepository>();
builder.Services.AddScoped<LayoutService>();

// ─── UI ───────────────────────────────────────────────────────────────────────
builder.Services.AddMudServices();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ─── Build ────────────────────────────────────────────────────────────────────
WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();
app.UseAntiforgery();

app.MapControllers();
app.MapStaticAssets();
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.Run();
