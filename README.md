# Enterprise ERP Development with Blazor SSR & Clean Architecture
<!-- Optimized for .NET 10, Blazor Static SSR, Enhanced Navigation, MudBlazor, and QuickGrid -->

## Table of Contents

1. [Introduction & Philosophy](#introduction)
2. [Prerequisites](#prerequisites)
3. [Architecture Overview](#architecture-overview)
4. [Project Setup](#project-setup)
   - 4.1 [Multi-Project Orchestration (VS Code)](#multi-project-orchestration)
   - 4.2 [User Secrets — How They Work (Dev → Prod)](#user-secrets)
5. [The .NET Core (Built-in DI & Services)](#core-architecture)
6. [Domain & Abstraction Layer](#domain-layer)
7. [Infrastructure & Security](#infrastructure-layer)
8. [White-Labeling & UI System](#ui-system)
9. [Feature Implementation (The ERP)](#feature-implementation)
    - 9.1 [Account Controller Auth Flow](#account-controller)
10. [State Management (Reactive Services)](#state-management)
11. [Containerization (Podman/Docker)](#containerization)
12. [Running the Application](#running)
13. [References & Documentation](#references)
14. [Appendix: Unit Testing](#testing)
15. [Enterprise CI/CD Pipeline (GitHub Actions)](#cicd)
16. [Enterprise Privacy & Crawler Shield](#privacy-shield)
17. [Hosting on Azure App Service / Fly.io](#hosting)
18. [How to Debug](#debugging)
19. [Typography: Libre Franklin](#typography)
20. [ASP.NET Core Web API Gateway (DummyJSON Wrapper)](#web-api-gateway)

---

## 1. Introduction & Philosophy <a name="introduction"></a>

This guide builds a **lightweight, high-performance ERP Dashboard** using **Blazor Static Server-Side Rendering (SSR)** in .NET 10. The stack is: Blazor SSR with **Enhanced Navigation**, **MudBlazor** for UI components, **QuickGrid** for data grids, the built-in **`<Virtualize>`** component for large lists, **`EditForm`** for type-safe form management, and **[DummyJSON](https://dummyjson.com)** as the API backend.

This is a genuine .NET-native implementation — not a JavaScript project in disguise. Every tool is a first-class citizen of the ASP.NET Core ecosystem.

> [!IMPORTANT]
> **Why Blazor Static SSR over Blazor Server or WASM?**
>
> Blazor Static SSR renders pages on the server and delivers fully-formed HTML on first load — giving you **superior SEO**, **faster Time-to-First-Byte**, and **no JavaScript bundle** for initial paint. Enhanced Navigation (`.NET 8+`) then intercepts subsequent link clicks and performs lightweight DOM-diffing updates, giving users the feel of a SPA without the SPA runtime. For customer-facing ERP portals that need to be discoverable and fast on every device, Static SSR is the correct default. Opt into `@rendermode InteractiveServer` selectively, only for components that need real-time interactivity (e.g., a live dashboard chart), and keep everything else static.

### The "Zero-Bloat" Promise

- **Strictly Open Source**: No Azure-paid tiers required — self-hostable on any OCI-compatible runtime.
- **Nullable Reference Types enforced**: `<Nullable>enable</Nullable>` and `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` throughout.
- **Built-in Dependency Injection**: .NET's first-party `IServiceCollection` / `IServiceProvider` — no custom container needed.
- **Clean Architecture**: Separation of concerns into `Core`, `Infrastructure`, and `Presentation` (`Components`) layers.
- **SEO-First**: Every page ships with `<PageTitle>`, `<HeadContent>`, structured meta tags, and a `robots.txt` — all rendered server-side.

---

## 2. Prerequisites <a name="prerequisites"></a>

- **.NET 10 SDK** (download from [dot.net](https://dot.net))
- **Podman** (or Docker Desktop)
- **Visual Studio 2022 17.8+** or **VS Code** with the C# Dev Kit extension
- **Git**

Verify your environment:

```bash
dotnet --version
# 10.0.x

podman -v
# podman version 5.x.x
```

---

## 3. Architecture Overview <a name="architecture-overview"></a>

We enforce a strict separation of concerns following Clean Architecture. Blazor pages live in the `Presentation` layer and call into `Core` abstractions; the `Infrastructure` layer provides concrete implementations.

```text
ErpPortal/
├── Components/               # Presentation layer (Blazor pages & components)
│   ├── Pages/                # @page routed components (the "View")
│   │   ├── Dashboard.razor
│   │   ├── Login.razor
│   │   ├── Users/
│   │   │   └── Index.razor
│   │   └── Tasks/
│   │       └── Index.razor
│   ├── Layout/               # App shell, nav menu, error boundaries
│   │   ├── MainLayout.razor
│   │   └── NavMenu.razor
│   └── App.razor             # Router root — Enhanced Navigation configured here
├── Core/                     # Pure business logic (no Blazor, no HTTP)
│   ├── Contracts/            # Interfaces (IRepository, IAuthService, etc.)
│   ├── Domain/               # C# records (User, Todo)
│   ├── Exceptions/           # AppException
│   └── Extensions/           # Extension methods / utilities
├── Infrastructure/           # External communications
│   ├── Http/                 # Typed HttpClient + DelegatingHandlers
│   ├── Repositories/         # Concrete IRepository<T> implementations
│   └── Services/             # AuthService, LayoutService, WhitelabelService
├── wwwroot/                  # Static assets
│   ├── css/app.css
│   └── robots.txt
├── appsettings.json          # Configuration (replaces .env)
├── appsettings.Development.json
└── Program.cs                # DI composition root + middleware pipeline
```

**.NET Library Responsibilities:**

| Library / Feature | Role |
|---|---|
| **Blazor `@page` routing** | File-based routing, route parameters, `[Authorize]` guards |
| **Enhanced Navigation** | SPA-like DOM-diff navigation without a client-side router |
| **`IMemoryCache` + `IOutputCacheStore`** | Server-state caching (replaces TanStack Query's stale time) |
| **`EditForm` + `DataAnnotationsValidator`** | Type-safe form management, server-side validation |
| **`QuickGrid<T>`** | Headless server-side data grid: sorting, filtering, pagination |
| **`<Virtualize<T>>`** | Windowed rendering for large lists (exact equivalent of `Virtualize` in Blazor) |

---

## 4. Project Setup <a name="project-setup"></a>

Initialize a Blazor Web App targeting Static SSR with no interactivity by default.

```bash
dotnet new blazor -n ErpPortal --interactivity None --empty
cd ErpPortal
```

> [!NOTE]
> **`--interactivity None`**
>
> This creates a pure Static SSR project. Interactivity is opt-in per component via `@rendermode InteractiveServer`. This is the correct default for a customer-facing ERP where SEO and TTFB matter most.

### Add NuGet Packages

```bash
# UI component library (Mantine equivalent)
dotnet add package MudBlazor

# Data grid (TanStack Table equivalent — ships with .NET 8+)
dotnet add package Microsoft.AspNetCore.Components.QuickGrid

# HTTP client JSON helpers
dotnet add package Microsoft.Extensions.Http
# dotnet add package Microsoft.Extensions.Logging
# NOTE: Do NOT add Microsoft.Extensions.Logging to Web/API projects. 
# It is included in the ASP.NET Core shared framework.

# Fluent validation (optional, for complex forms)
dotnet add package FluentValidation.DependencyInjectionExtensions

# Output caching middleware
# (included in Microsoft.AspNetCore.OutputCaching — no extra package needed for .NET 10)

# Structured logging with Serilog
dotnet add package Serilog.AspNetCore

# Unit testing (see Appendix)
dotnet add package xunit --project ErpPortal.Tests
dotnet add package Moq --project ErpPortal.Tests
dotnet add package FluentAssertions --project ErpPortal.Tests
```

### Configure the Project File (`ErpPortal.csproj`)

Replace the default contents with a strict, production-grade configuration:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <!-- Fail the build on any warning — the "No any" equivalent for C# -->
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsAsErrors />
    <RootNamespace>ErpPortal</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MudBlazor" Version="7.*" />
    <PackageReference Include="Microsoft.AspNetCore.Components.QuickGrid" Version="10.*" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.*" />
    <!-- <PackageReference Include="Microsoft.Extensions.Logging" Version="10.*" /> -->
    <PackageReference Include="Serilog.AspNetCore" Version="9.*" />
  </ItemGroup>

</Project>
```

> [!TIP]
> **Build Strategy: Treating Warnings as Errors**
>
> The equivalent of TypeScript's `strict: true` in .NET is `<Nullable>enable</Nullable>` combined with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`. This prevents nullable dereferences, unused variables, and unhandled cases from silently shipping to production.

### Configuration (`appsettings.json`)

Replaces `.env` files. Values are validated at startup via `IOptions` binding with `ValidateDataAnnotations()`.

```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:5002/api"
  },
  "Branding": {
    "CompanyName": "Acme Corp ERP",
    "LogoUrl": "https://dummyjson.com/icon/acme/128",
    "PrimaryColor": "#0052cc",
    "SecondaryColor": "#172b4d",
    "AccentColor": "#ffab00"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

Create `appsettings.Development.json` for local overrides (equivalent to `.env.local`):

```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:5002/api"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

> [!CAUTION]
> **Environment-Specific Secrets**
>
> Never commit API keys or tokens to `appsettings.json`. Use `dotnet user-secrets` locally and **GitHub Secrets** / environment variables in CI. ASP.NET Core automatically reads `ASPNETCORE_ApiSettings__BaseUrl` (double underscore as separator) from environment variables, overriding `appsettings.json`.

```bash
# Local development secrets (stored outside the project directory)
dotnet user-secrets init
dotnet user-secrets set "ApiSettings:BaseUrl" "https://your-api.example.com"
```

---

### 4.1 Multi-Project Orchestration (VS Code) <a name="multi-project-orchestration"></a>

To strictly enforce that the API Gateway is running before the Blazor App starts, use a shared `.vscode` setup with a sequenced task pipeline.

#### `.vscode/tasks.json`
```json
{
  "version": "2.0.0",
  "tasks": [
    { "label": "build-api", "command": "dotnet", "type": "process", "args": ["build", "${workspaceFolder}/ErpPortal.Api/ErpPortal.Api.csproj"] },
    { "label": "build-app", "command": "dotnet", "type": "process", "args": ["build", "${workspaceFolder}/ErpPortal/ErpPortal.csproj"] },
    {
      "label": "ready-frontend",
      "dependsOrder": "sequence",
      "dependsOn": ["build-app", "wait-for-api"]
    },
    {
      "label": "wait-for-api",
      "type": "shell",
      "command": "${workspaceFolder}/.vscode/wait-for-port.sh",
      "args": ["localhost", "5002"]
    }
  ]
}
```

#### `.vscode/launch.json`
```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Backend: API Gateway",
      "type": "dotnet",
      "request": "launch",
      "projectPath": "${workspaceFolder}/ErpPortal.Api/ErpPortal.Api.csproj",
      "preLaunchTask": "build-api",
      "env": { "ASPNETCORE_URLS": "http://localhost:5002" }
    },
    {
      "name": "Frontend: Blazor Portal",
      "type": "dotnet",
      "request": "launch",
      "projectPath": "${workspaceFolder}/ErpPortal/ErpPortal.csproj",
      "preLaunchTask": "ready-frontend",
      "env": { "ASPNETCORE_URLS": "http://localhost:5000" }
    }
  ],
  "compounds": [
    {
      "name": "Full ERP Solution",
      "configurations": ["Backend: API Gateway", "Frontend: Blazor Portal"],
      "stopAll": true
    }
  ]
}
```

---

### 4.2 User Secrets — How They Work (Dev → Prod) <a name="user-secrets"></a>

#### What Are User Secrets?

`dotnet user-secrets` is the ASP.NET Core mechanism for keeping sensitive configuration values **off disk and out of source control** during local development. Secrets are stored in a JSON file in your OS user profile directory — completely outside the project folder — and are never committed to Git.

| Environment | Where secrets live | How ASP.NET Core reads them |
|---|---|---|
| **Development** | `%APPDATA%\Microsoft\UserSecrets\<guid>\secrets.json` (Windows) or `~/.microsoft/usersecrets/<guid>/secrets.json` (Linux/macOS) | Automatically loaded when `ASPNETCORE_ENVIRONMENT=Development` |
| **CI / Staging** | GitHub Secrets / Azure DevOps variable groups | Injected as environment variables at pipeline runtime |
| **Production** | Azure Key Vault (recommended) or environment variables | Loaded via `AddAzureKeyVault()` or read from process environment |

The `<guid>` is the `UserSecretsId` declared in the `.csproj`:

```xml
<PropertyGroup>
  <UserSecretsId>your-project-guid-here</UserSecretsId>
</PropertyGroup>
```

ASP.NET Core's configuration builder layers sources in priority order (last one wins):

```
appsettings.json
  ↓ overridden by
appsettings.Development.json
  ↓ overridden by
User Secrets  (Development only)
  ↓ overridden by
Environment Variables  (all environments)
  ↓ overridden by
Command-line arguments
```

This means the same key — e.g. `ApiSettings:BaseUrl` — can exist in `appsettings.json` as a safe default and be silently overridden by a secret without touching any file tracked by Git.

#### Setting Up User Secrets (Blazor WebUI)

```powershell
# 1. Initialise — adds <UserSecretsId> to the .csproj if not already present
dotnet user-secrets init --project ErpPortal/ErpPortal.csproj

# 2. Set a secret value
dotnet user-secrets set "ApiSettings:BaseUrl" "https://your-api.example.com" --project ErpPortal/ErpPortal.csproj

# 3. List all stored secrets for this project
dotnet user-secrets list --project ErpPortal/ErpPortal.csproj

# 4. Remove a specific secret
dotnet user-secrets remove "ApiSettings:BaseUrl" --project ErpPortal/ErpPortal.csproj

# 5. Clear all secrets for this project
dotnet user-secrets clear --project ErpPortal/ErpPortal.csproj
```

#### Setting Up User Secrets (API Gateway)

```powershell
# The JWT signing secret must never appear in appsettings.json
# Generate a cryptographically random 256-bit (32-byte) secret

$rng   = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$bytes = New-Object byte[] 32
$rng.GetBytes($bytes)
$secret = [System.Convert]::ToBase64String($bytes)

dotnet user-secrets set "Jwt:Secret" $secret --project ErpPortal.Api/ErpPortal.Api.csproj
```

`secrets.json` on disk looks identical to `appsettings.json` — nested keys use JSON objects, not `__` separators:

```json
{
  "Jwt": {
    "Secret": "your-base64-encoded-256bit-secret"
  },
  "ApiSettings": {
    "BaseUrl": "https://your-api.example.com"
  }
}
```

> [!NOTE]
> **No Code Changes Required**
>
> `IConfiguration` reads user secrets transparently. `builder.Configuration["Jwt:Secret"]` and `IOptions<ApiSettings>` work identically whether the value comes from `appsettings.json`, user secrets, or an environment variable. The application code has zero awareness of the storage location.

#### How User Secrets Are Enabled in `Program.cs`

The Web Application builder enables user secrets automatically in Development — no explicit call needed:

```csharp
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
// User secrets are loaded automatically when:
// - builder.Environment.IsDevelopment() is true
// - The .csproj has a <UserSecretsId> element
// No additional setup required.
```

If you need secrets in non-Development environments (e.g., a local Staging run):

```csharp
builder.Configuration.AddUserSecrets<Program>(optional: true);
```

#### Production: Environment Variables

In production ASP.NET Core reads the same keys from **environment variables**. The only difference is that nested JSON paths use `__` (double underscore) as the separator:

```
appsettings.json key     →  Environment variable
─────────────────────────────────────────────────
Jwt:Secret               →  Jwt__Secret
ApiSettings:BaseUrl      →  ApiSettings__BaseUrl
DummyJson:Username       →  DummyJson__Username
```

Set them in your hosting platform:

```powershell
# Azure App Service — via Azure CLI
az webapp config appsettings set \
  --name my-erp-portal \
  --resource-group my-rg \
  --settings Jwt__Secret="..." ApiSettings__BaseUrl="https://api.prod.example.com"

# Docker / Podman — passed at container run time
podman run -d \
  -e Jwt__Secret="..." \
  -e ApiSettings__BaseUrl="https://api.prod.example.com" \
  enterprise-erp-portal
```

#### Production: Azure Key Vault (Recommended)

For enterprise workloads, store secrets in Azure Key Vault and load them at startup. Key Vault secret names use `--` (double dash) in place of `:` because Key Vault names cannot contain colons:

```powershell
# Create the secret in Key Vault
az keyvault secret set --vault-name my-kv --name "Jwt--Secret" --value "your-secret"
```

```csharp
// Program.cs — load Key Vault secrets before DI registration
if (!builder.Environment.IsDevelopment())
{
    string kvUri = builder.Configuration["KeyVault:Uri"]
        ?? throw new InvalidOperationException("KeyVault:Uri is required in production.");

    builder.Configuration.AddAzureKeyVault(
        new Uri(kvUri),
        new DefaultAzureCredential());   // Uses Managed Identity in Azure — no credentials in code
}
```

Add the NuGet package:

```bash
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
dotnet add package Azure.Identity
```

> [!TIP]
> **Managed Identity — Zero Credential Rotation**
>
> When running on Azure App Service or Azure Container Apps, `DefaultAzureCredential()` authenticates to Key Vault using the service's **Managed Identity** — no client secret or certificate required in the application. Key Vault access policies (or RBAC) control which identities can read which secrets. This is the production-grade zero-secret-in-code pattern.

#### Summary: Secret Storage Decision Tree

```
Is this a secret value?
│
├── No  → appsettings.json (safe to commit)
│
└── Yes
    ├── Local development  → dotnet user-secrets
    ├── CI pipeline        → GitHub Secrets / Azure DevOps variable group
    ├── Container hosting  → Environment variable at container run time
    └── Azure hosting      → Azure Key Vault + Managed Identity (preferred)
```

---

## 5. The .NET Core (Built-in DI & Services) <a name="core-architecture"></a>

Unlike the JavaScript implementation which required a hand-rolled Service Locator, .NET ships a production-grade IoC container. Services are registered in `Program.cs` and injected via `@inject` in Blazor components — exactly the same mental model as Blazor's `@inject` directive has always promised.

### Options Validation (`Core/Config/ApiSettings.cs`)

Validates configuration at startup — causing a fast crash if required values are missing, equivalent to the Zod validation in the original.

```csharp
// Core/Config/ApiSettings.cs
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;

namespace ErpPortal.Core.Config;

public sealed class ApiSettings
{
    public const string SectionName = "ApiSettings";

    [Required, Url]
    [ConfigurationKeyName("BaseUrl")]
    public string BaseUrl { get; init; } = string.Empty;
}
```

Register with eager validation in `Program.cs`:

```csharp
builder.Services
    .AddOptions<ApiSettings>()
    .BindConfiguration(ApiSettings.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart(); // Fast crash if config is invalid — equivalent to Zod's safeParse throw
```

---

## 6. Domain & Abstraction Layer <a name="domain-layer"></a>

Define **WHAT** the system does, not **HOW**. C# `record` types replace Zod schemas as the single source of truth for domain entities. They are immutable, structurally comparable, and serialization-friendly.

### `Core/Domain/User.cs`

```csharp
using System.Text.Json.Serialization;

namespace ErpPortal.Core.Domain;

/// <summary>
/// Immutable domain entity. Replaces the Zod-inferred TypeScript type.
/// Records give value-equality semantics for free.
/// </summary>
public sealed record User(
    int Id,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string Image,
    // DummyJSON returns the token as "accessToken" — JsonPropertyName maps it to this property
    [property: JsonPropertyName("accessToken")] string? Token = null
);
```

### `Core/Domain/Todo.cs`

```csharp
using System.Text.Json.Serialization;

namespace ErpPortal.Core.Domain;

public sealed record Todo(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("todo")] string TodoText,
    [property: JsonPropertyName("completed")] bool Completed,
    [property: JsonPropertyName("userId")] int UserId
);
```

### `Core/Contracts/ILogger.cs`

> [!NOTE]
> **Use `Microsoft.Extensions.Logging.ILogger<T>`**
>
> Do not define a custom `ILogger` interface. .NET ships `ILogger<T>` which is generic, structured, and supports multiple sinks (Console, Application Insights, Sentry) without any business logic changes. Simply inject `ILogger<MyService>` and swap sinks via `appsettings.json` configuration.

In ASP.NET Web API (and ASP.NET Core), logging and Dependency Injection (DI) go hand-in-hand. You don't need to manually instantiate a logger; instead, you request it in your class's constructor.

#### 1. The Standard Pattern (Constructor Injection)
To use logging in any class (a Service, Repository, or Controller), inject the generic `ILogger<T>` interface. The `<T>` tells the logger which class is reporting the message, which is vital for filtering logs later.

```csharp
using Microsoft.Extensions.Logging;

public class MyBusinessService
{
    private readonly ILogger<MyBusinessService> _logger;

    // The DI container automatically provides the logger instance
    public MyBusinessService(ILogger<MyBusinessService> logger)
    {
        _logger = logger;
    }

    public void ProcessData()
    {
        _logger.LogInformation("Processing data at {Time}", DateTime.UtcNow);
        
        try 
        {
            // logic here
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing data.");
        }
    }
}
```

#### 2. Using Modern C# 12+ Primary Constructors
If you are using .NET 8, 9, or 10, you can use **Primary Constructors** to remove the boilerplate code of assigning private fields.

```csharp
public class MyBusinessService(ILogger<MyBusinessService> logger)
{
    public void ProcessData()
    {
        logger.LogInformation("Clean and concise logging!");
    }
}
```

---

#### 3. Registering Your Class
For DI to work, your class must be registered in the service container. This is done in `Program.cs`.

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register your class so the DI container knows how to create it
builder.Services.AddScoped<MyBusinessService>(); 

var app = builder.Build();
```

#### 3.1 Service Lifetimes (Transient vs. Scoped vs. Singleton)

When registering a service, you must specify its **lifetime**, which dictates how often the DI container creates a new instance.

| Lifetime | Method | How it works | App to Client (e.g., Web API) | App to App (e.g., Background Workers / Queue Consumers) | When to Use |
|---|---|---|---|---|---|
| **Transient** | `AddTransient<T>()` | A new instance is created **every time** you request it. | A separate instance is given to every class that injects it, even within the same HTTP request. | A separate instance is given to every class processing a message. | Lightweight, stateless services or fast execution helpers. |
| **Scoped** | `AddScoped<T>()` | A new instance is created **once per scope**. | **Once per HTTP request.** All classes handling the same HTTP request share the same instance. | **Once per message/job.** You must manually create a scope (`CreateScope()`) for each unit of work. | Database contexts (`DbContext`), Repositories, HTTP Clients, or services holding request-state. |
| **Singleton** | `AddSingleton<T>()` | A single instance is created **once** and shared forever. | Shared across **all** clients and all HTTP requests. | Shared globally across the entire application lifecycle. | In-memory caches, configuration services, or expensive connections (e.g., Redis Multiplexer). |

> [!CAUTION]
> **The Captive Dependency Problem:** Never inject a **Scoped** service into a **Singleton** service. The Singleton will "trap" the Scoped service, effectively turning it into a Singleton. This causes major bugs with stateful services like `DbContext`!

---

#### 4. Critical Best Practices for 2026

* **Structured Logging (Message Templates):** Never use string interpolation (e.g., `$"User {id} logged in"`). Use message templates (e.g., `"User {UserId} logged in", id`). This allows tools like Serilog or Application Insights to treat `{UserId}` as a searchable property rather than just a flat string.
* **Log Levels:** Use them correctly so you can filter noise in production:
    * `LogTrace`/`LogDebug`: Deep troubleshooting (usually off in production).
    * `LogInformation`: High-level flow (user login, service started).
    * `LogWarning`: Something unexpected happened, but the app is still running.
    * `LogError`: A specific operation failed.
    * `LogCritical`: The whole app or a major component crashed.
* **Avoid Static Loggers:** Resist the urge to use a static `LogManager`. Injecting `ILogger<T>` makes your code unit-testable because you can easily mock the logger in your tests.
* **Direct Framework Logging:** Do not add `Microsoft.Extensions.Logging` to WebAPI or Blazor Web projects. Use `ILogger<T>` directly; it is already available in the ASP.NET Core shared framework.
* **Logging in Class Libraries:** If you need logging in a non-web class library (e.g., a shared Domain or Logic project), add `Microsoft.Extensions.Logging.Abstractions` there instead of the full logging implementation.

#### Example: Logging in a Non-Web Class Library (`Core.csproj`)

In a pure logic library (using `Microsoft.NET.Sdk`), you must explicitly add the abstractions package to access the logging interfaces.

```xml
<!-- ErpPortal.Core/ErpPortal.Core.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <!-- Provides ILogger without pulling in the entire hosting stack -->
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.*" />
  </ItemGroup>
</Project>
```

Usage in the library:

```csharp
namespace ErpPortal.Core.Services;
using Microsoft.Extensions.Logging;

public sealed class ProcessService(ILogger<ProcessService> logger)
{
    public void Run() => logger.LogInformation("Process started in the Core layer.");
}
```

#### Example: Web/API Project (Zero Setup)

In your WebAPI or Blazor project (`Microsoft.NET.Sdk.Web`), `ILogger<T>` is available globally. Do **not** add any logging NuGet packages unless you are adding a third-party sink like Serilog.

```csharp
// Controllers/OrderController.cs in ErpPortal.Api
[ApiController]
public class OrderController(ILogger<OrderController> logger) : ControllerBase
{
    [HttpGet]
    public IActionResult Get() 
    {
        logger.LogDebug("Fetching orders..."); 
        return Ok();
    }
}
```

#### 5. Advanced: Third-Party Providers
While the built-in logging is great, most production APIs in 2026 use **Serilog** for more advanced "Sinks" (sending logs to SQL, Seq, or Elasticsearch).

```csharp
// In Program.cs
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .WriteTo.Console());
```

Even if you use Serilog, your classes should still inject `ILogger<T>` from `Microsoft.Extensions.Logging`. This keeps your business logic decoupled from the specific logging library.

> [!TIP]
> **Serilog Documentation**
>
> For advanced configurations, Sinks (Seq, SQL, Elasticsearch), and Enrichment, refer to the [official Serilog documentation](https://serilog.net/) and the [Serilog.AspNetCore repository](https://github.com/serilog/serilog-aspnetcore).

### `Core/Exceptions/AppException.cs`

```csharp
namespace ErpPortal.Core.Exceptions;

public sealed class AppException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }
    public object? Context { get; }

    public AppException(
        string message,
        string code = "UNKNOWN_ERROR",
        int statusCode = 500,
        object? context = null)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
        Context = context;
    }
}
```

### `Core/Contracts/IErpHttpClient.cs`

```csharp
namespace ErpPortal.Core.Contracts;

public interface IErpHttpClient
{
    Task<T> GetAsync<T>(string url, CancellationToken ct = default) where T : class;
    Task<T> PostAsync<T>(string url, object data, CancellationToken ct = default) where T : class;
    Task<T> PutAsync<T>(string url, object data, CancellationToken ct = default) where T : class;
    Task DeleteAsync(string url, CancellationToken ct = default);
    void SetAuthToken(string? token);
}
```

### `Core/Contracts/IAuthService.cs`

```csharp
using ErpPortal.Core.Domain;

namespace ErpPortal.Core.Contracts;

public interface IAuthService
{
    Task<User> LoginAsync(string username, string password, CancellationToken ct = default);
    Task LogoutAsync();
    Task<User?> GetCurrentUserAsync();
    Task<bool> IsAuthenticatedAsync();
}
```

### `Core/Contracts/IRepository.cs`

```csharp
namespace ErpPortal.Core.Contracts;

public interface IRepository<T> where T : class
{
    Task<(IReadOnlyList<T> Data, int Total)> GetAllAsync(int skip = 0, int limit = 50, CancellationToken ct = default);
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<T> CreateAsync(T entity, CancellationToken ct = default);
    Task<T> UpdateAsync(int id, T entity, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
```

### `Core/Contracts/INotificationService.cs`

```csharp
namespace ErpPortal.Core.Contracts;

public interface INotificationService
{
    void ShowSuccess(string title, string message);
    void ShowError(string title, string message);
    void ShowInfo(string title, string message);
}
```

> [!TIP]
> **Architectural Pattern: Data Access Abstraction**
>
> The Repository pattern decouples business logic from the HTTP client. If you switch from `HttpClient` to a gRPC client, or REST to GraphQL, only the `Infrastructure` layer changes. Your domain logic and Blazor pages remain untouched.

---

## 7. Infrastructure & Security <a name="infrastructure-layer"></a>

Authentication in Blazor SSR uses **cookie-based sessions** managed by ASP.NET Core — this is more secure than the SPA approach of storing tokens in `localStorage`, since HTTP-only cookies are inaccessible to JavaScript.

### `Infrastructure/Http/AuthTokenHandler.cs`

A `DelegatingHandler` that automatically injects the portal JWT into every outgoing request to the API gateway, and **intercepts `401 Unauthorized` responses**. When the API rejects a request because the token has expired, the handler signs the user out of the cookie session and redirects to `/login?error=session_expired` — equivalent to an Axios response interceptor that handles expired tokens.

```csharp
using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ErpPortal.Infrastructure.Http;

/// <summary>
/// Injects the Bearer token from the authenticated user's claims into outgoing HTTP requests.
/// Equivalent of an Axios request interceptor.
/// If the API returns 401 (token expired), signs the user out and redirects to /login.
/// </summary>
public sealed class AuthTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthTokenHandler(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        string? token = _httpContextAccessor.HttpContext?.User.FindFirst("Token")?.Value;
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            HttpContext? ctx = _httpContextAccessor.HttpContext;
            if (ctx is not null)
            {
                await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                ctx.Response.Redirect("/login?error=session_expired");
            }
        }

        return response;
    }
}
```

> [!NOTE]
> **Expired Token Flow**
>
> 1. Portal JWT expires → WebAPI rejects the request with `401 Unauthorized`
> 2. `AuthTokenHandler` catches the `401`, calls `SignOutAsync` to clear the cookie, and redirects to `/login?error=session_expired`
> 3. The login page maps `session_expired` to *"Your session has expired. Please sign in again."*
>
> This prevents users from seeing an unhandled error state — they are returned cleanly to the login page.

### `Infrastructure/Http/ErrorHandlingHandler.cs`

Intercepts HTTP errors and maps them to `AppException`, then re-throws for the caller to handle. `INotificationService` is intentionally **not** injected here — `ISnackbar` (the MudBlazor implementation) depends on `NavigationManager`, which is not initialized during Static SSR when the HTTP handler pipeline is first constructed. Pages catch `AppException` and call `INotificationService` directly.

```csharp
using ErpPortal.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace ErpPortal.Infrastructure.Http;

public sealed class ErrorHandlingHandler : DelegatingHandler
{
    private readonly ILogger<ErrorHandlingHandler> _logger;

    public ErrorHandlingHandler(ILogger<ErrorHandlingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[API Error] Network failure calling {Url}", request.RequestUri);
            throw new AppException(ex.Message, "NETWORK_ERROR", 0);
        }

        if (!response.IsSuccessStatusCode)
        {
            int status = (int)response.StatusCode;
            AppException appError = status switch
            {
                401 => new AppException("Unauthorized", "AUTH_401", 401),
                500 => new AppException("Server Error", "SERVER_500", 500),
                _   => new AppException($"HTTP {status}", $"HTTP_{status}", status),
            };

            _logger.LogError("[API Error] {Code} — {Url}", appError.Code, request.RequestUri);
            throw appError;
        }

        return response;
    }
}
```

### `Infrastructure/Http/ErpHttpClient.cs`

```csharp
using System.Net.Http.Json;
using System.Text.Json;
using ErpPortal.Core.Contracts;

namespace ErpPortal.Infrastructure.Http;

public sealed class ErpHttpClient : IErpHttpClient
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public ErpHttpClient(HttpClient http) => _http = http;

    public async Task<T> GetAsync<T>(string url, CancellationToken ct = default) where T : class
    {
        T? result = await _http.GetFromJsonAsync<T>(url, _jsonOptions, ct);
        return result ?? throw new InvalidOperationException($"Null response from GET {url}");
    }

    public async Task<T> PostAsync<T>(string url, object data, CancellationToken ct = default) where T : class
    {
        HttpResponseMessage response = await _http.PostAsJsonAsync(url, data, _jsonOptions, ct);
        response.EnsureSuccessStatusCode();
        T? result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, ct);
        return result ?? throw new InvalidOperationException($"Null response from POST {url}");
    }

    public async Task<T> PutAsync<T>(string url, object data, CancellationToken ct = default) where T : class
    {
        HttpResponseMessage response = await _http.PutAsJsonAsync(url, data, _jsonOptions, ct);
        response.EnsureSuccessStatusCode();
        T? result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, ct);
        return result ?? throw new InvalidOperationException($"Null response from PUT {url}");
    }

    public async Task DeleteAsync(string url, CancellationToken ct = default)
    {
        HttpResponseMessage response = await _http.DeleteAsync(url, ct);
        response.EnsureSuccessStatusCode();
    }

    // In Blazor SSR the token is injected by AuthTokenHandler — this is a no-op here
    // but satisfies the interface for testability.
    public void SetAuthToken(string? token) { }
}
```

### `Infrastructure/Services/AuthService.cs`

Server-side cookie authentication — HTTP-only cookies replace the `localStorage` approach from the SPA version. This is more secure and eliminates token rehydration edge cases.

> [!NOTE]
> In the current implementation, the primary sign-in/sign-out flow is the controller POST path (`/account/login`, `/account/logout`) from `Login.razor`.
> `AuthService` remains useful for programmatic auth operations, user-session helpers (`GetCurrentUserAsync`, `IsAuthenticatedAsync`), and integration scenarios outside the form-post login route.

```csharp
using System.Security.Claims;
using ErpPortal.Core.Contracts;
using ErpPortal.Core.Domain;
using ErpPortal.Core.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;

namespace ErpPortal.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly IErpHttpClient _http;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly INotificationService _notifier;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IErpHttpClient http,
        IHttpContextAccessor httpContextAccessor,
        INotificationService notifier,
        ILogger<AuthService> logger)
    {
        _http = http;
        _httpContextAccessor = httpContextAccessor;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task<User> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        _logger.LogInformation("Login attempt for {Username}", username);
        try
        {
            User user = await _http.PostAsync<User>("/auth/login",
                new { username, password }, ct);

            List<Claim> claims =
            [
                new(ClaimTypes.Name,  user.Username),
                new(ClaimTypes.Email, user.Email),
                new("FirstName",      user.FirstName),
                new("LastName",       user.LastName),
                new("Token",          user.Token ?? string.Empty),
            ];

            ClaimsIdentity identity   = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            ClaimsPrincipal principal  = new(identity);

            await _httpContextAccessor.HttpContext!.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc   = DateTimeOffset.UtcNow.AddHours(1),
                });

            _notifier.ShowSuccess("Welcome", $"Logged in as {user.FirstName} {user.LastName}");
            _logger.LogInformation("Login successful for {Username}", username);
            return user;
        }
        catch (AppException ex)
        {
            string message = ex.StatusCode switch
            {
                401 => "Invalid username or password.",
                403 => "Login request was blocked by the upstream service.",
                0   => "Could not reach the authentication service.",
                _   => $"Login failed ({ex.Code}).",
            };

            _notifier.ShowError("Login Failed", message);
            _logger.LogWarning("Login failed for {Username}. Code: {Code}, Status: {Status}", username, ex.Code, ex.StatusCode);
            throw;
        }
        catch (Exception ex)
        {
            _notifier.ShowError("Login Failed", "An unexpected error occurred during login.");
            _logger.LogError(ex, "Unexpected error during login for {Username}", username);
            throw;
        }
    }

    public async Task LogoutAsync()
    {
        await _httpContextAccessor.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _notifier.ShowInfo("Logged Out", "You have been safely signed out.");
        _logger.LogInformation("User logged out");
    }

    public Task<User?> GetCurrentUserAsync()
    {
        HttpContext? ctx = _httpContextAccessor.HttpContext;
        if (ctx?.User.Identity?.IsAuthenticated is not true) return Task.FromResult<User?>(null);

        User user = new User(
            Id:        0,
            Username:  ctx.User.Identity.Name ?? string.Empty,
            Email:     ctx.User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
            FirstName: ctx.User.FindFirst("FirstName")?.Value ?? string.Empty,
            LastName:  ctx.User.FindFirst("LastName")?.Value ?? string.Empty,
            Image:     string.Empty,
            Token:     ctx.User.FindFirst("Token")?.Value);

        return Task.FromResult<User?>(user);
    }

    public Task<bool> IsAuthenticatedAsync()
        => Task.FromResult(_httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated is true);
}
```

### `Infrastructure/Services/ConsoleLogger`

> [!NOTE]
> **Use the Built-in Logger**
>
> .NET's `ILogger<T>` writes structured JSON to the console in production automatically when the `Console` formatter is set to `json`. There is no `ConsoleLogger.cs` to write — simply inject `ILogger<MyService>` and configure the formatter in `appsettings.json`. To swap to Sentry or Datadog, add the sink package and configure it in `Program.cs` — no business logic changes required.

```json
// appsettings.Production.json — structured JSON logging
{
  "Logging": {
    "Console": {
      "FormatterName": "json"
    }
  }
}
```

### `Infrastructure/Repositories/UserRepository.cs`

```csharp
using ErpPortal.Core.Contracts;
using ErpPortal.Core.Domain;
using ErpPortal.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace ErpPortal.Infrastructure.Repositories;

// Internal DTO for deserializing the paginated API response
internal sealed record UsersApiResponse(List<User> Users, int Total);

public sealed class UserRepository : IRepository<User>
{
    private readonly IErpHttpClient _http;
    private readonly ILogger<UserRepository> _logger;
    private readonly INotificationService _notifier;

    public UserRepository(IErpHttpClient http, ILogger<UserRepository> logger, INotificationService notifier)
    {
        _http = http;
        _logger = logger;
        _notifier = notifier;
    }

    public async Task<(IReadOnlyList<User> Data, int Total)> GetAllAsync(
        int skip = 0, int limit = 50, CancellationToken ct = default)
    {
        try
        {
            // Now calling the API Gateway's /products proxy instead of direct /users
            UsersApiResponse response = await _http.GetAsync<UsersApiResponse>($"/products?limit={limit}&skip={skip}", ct);
            return (response.Users, response.Total);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "CRITICAL: User Fetch — {Message}", e.Message);
            throw;
        }
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        try { return await _http.GetAsync<User>($"/products/{id}", ct); }
        catch (Exception e) { _logger.LogError(e, "Failed to fetch user {Id}", id); throw; }
    }

    public async Task<User> CreateAsync(User entity, CancellationToken ct = default)
    {
        try
        {
            User user = await _http.PostAsync<User>("/users/add", entity, ct);
            _notifier.ShowSuccess("User Created", $"{user.FirstName} has been added.");
            return user;
        }
        catch (Exception e) { _logger.LogError(e, "Failed to create user"); throw; }
    }

    public async Task<User> UpdateAsync(int id, User entity, CancellationToken ct = default)
    {
        try
        {
            User user = await _http.PutAsync<User>($"/users/{id}", entity, ct);
            _notifier.ShowSuccess("User Updated", $"Profile for {user.FirstName} saved.");
            return user;
        }
        catch (Exception e) { _logger.LogError(e, "Failed to update user {Id}", id); throw; }
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        try
        {
            await _http.DeleteAsync($"/users/{id}", ct);
            _notifier.ShowSuccess("User Deleted", "The record has been permanently removed.");
        }
        catch (Exception e) { _logger.LogError(e, "Failed to delete user {Id}", id); throw; }
    }
}
```

### `Infrastructure/Repositories/TodoRepository.cs`

```csharp
using ErpPortal.Core.Contracts;
using ErpPortal.Core.Domain;
using Microsoft.Extensions.Logging;

namespace ErpPortal.Infrastructure.Repositories;

internal sealed record TodosApiResponse(List<Todo> Todos, int Total);

public sealed class TodoRepository : IRepository<Todo>
{
    private readonly IErpHttpClient _http;
    private readonly ILogger<TodoRepository> _logger;
    private readonly INotificationService _notifier;

    public TodoRepository(IErpHttpClient http, ILogger<TodoRepository> logger, INotificationService notifier)
    {
        _http = http; _logger = logger; _notifier = notifier;
    }

    public async Task<(IReadOnlyList<Todo> Data, int Total)> GetAllAsync(
        int skip = 0, int limit = 150, CancellationToken ct = default)
    {
        try
        {
            // Now calling the API Gateway's /todos proxy
            TodosApiResponse response = await _http.GetAsync<TodosApiResponse>($"/todos?limit={limit}&skip={skip}", ct);
            return (response.Todos, response.Total);
        }
        catch (Exception e) { _logger.LogError(e, "Failed to fetch todos"); throw; }
    }

    public async Task<Todo?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _http.GetAsync<Todo>($"/todos/{id}", ct);

    public async Task<Todo> CreateAsync(Todo entity, CancellationToken ct = default)
    {
        Todo todo = await _http.PostAsync<Todo>("/todos/add", entity, ct);
        _notifier.ShowSuccess("Task Created", "New task added.");
        return todo;
    }

    public async Task<Todo> UpdateAsync(int id, Todo entity, CancellationToken ct = default)
    {
        Todo todo = await _http.PutAsync<Todo>($"/todos/{id}", entity, ct);
        _notifier.ShowSuccess("Task Updated", "Changes saved.");
        return todo;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await _http.DeleteAsync($"/todos/{id}", ct);
        _notifier.ShowSuccess("Task Deleted", "The task has been removed.");
    }
}
```

---

## 8. White-Labeling & UI System <a name="ui-system"></a>

### `Core/Config/BrandingConfig.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace ErpPortal.Core.Config;

public sealed class BrandingConfig
{
    public const string SectionName = "Branding";

    [Required] public string CompanyName  { get; init; } = "Enterprise ERP";
    [Required, Url] public string LogoUrl { get; init; } = string.Empty;
    public string PrimaryColor   { get; init; } = "#0052cc";
    public string SecondaryColor { get; init; } = "#172b4d";
    public string AccentColor    { get; init; } = "#ffab00";
}
```

### `Infrastructure/Services/LayoutService.cs`

Replaces the `BaseObservable<LayoutState>` reactive service. In Blazor, components subscribe to the service's `OnChange` event and call `StateHasChanged()` — the same observable pattern, using idiomatic .NET events.

```csharp
namespace ErpPortal.Infrastructure.Services;

/// <summary>
/// Scoped service managing transient UI state.
/// Equivalent of the TypeScript BaseObservable&lt;LayoutState&gt; reactive service.
/// Components subscribe to OnChange and call StateHasChanged() — Blazor's useSyncExternalStore.
/// </summary>
public sealed class LayoutService
{
    // MudBlazor v9: @bind-Open on MudDrawer requires a public setter.
    // A backing field is used so the setter can also fire OnChange (CS0272 fix).
    private bool _isSidebarOpen;

    public bool IsSidebarOpen
    {
        get => _isSidebarOpen;
        set
        {
            if (_isSidebarOpen == value) return;
            _isSidebarOpen = value;
            NotifyStateChanged();
        }
    }

    // The Action-based event replaces the Set<Listener> in BaseObservable
    public event Action? OnChange;

    public void ToggleSidebar()
    {
        IsSidebarOpen = !IsSidebarOpen;
    }

    public void CloseSidebar()
    {
        IsSidebarOpen = false;
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
```

### `Infrastructure/Services/MudBlazorNotificationService.cs`

```csharp
using ErpPortal.Core.Contracts;
using MudBlazor;

namespace ErpPortal.Infrastructure.Services;

/// <summary>
/// MudBlazor implementation of INotificationService.
/// Equivalent of MantineNotificationService — swap out by registering a different
/// implementation in Program.cs without touching any business logic.
/// </summary>
public sealed class MudBlazorNotificationService : INotificationService
{
    private readonly ISnackbar _snackbar;

    public MudBlazorNotificationService(ISnackbar snackbar) => _snackbar = snackbar;

    public void ShowSuccess(string title, string message)
        => _snackbar.Add($"{title}: {message}", Severity.Success);

    public void ShowError(string title, string message)
        => _snackbar.Add($"{title}: {message}", Severity.Error);

    public void ShowInfo(string title, string message)
        => _snackbar.Add($"{title}: {message}", Severity.Info);
}
```

### MudBlazor Theme Provider (`Components/Layout/ThemeProvider.razor`)

MudBlazor's `MudThemeProvider` is the direct equivalent of the Mantine `createTheme` + `MantineProvider` wrapper. CSS custom properties are synchronized at the root for third-party component compatibility.

```razor
@using Microsoft.Extensions.Options
@using MudBlazor.Utilities
@inject IOptions<BrandingConfig> BrandingOptions

@* Equivalent of EnterpriseThemeProvider.tsx — sets the MudBlazor theme and
   synchronises CSS variables at the document root for third-party component compatibility. *@

<MudThemeProvider Theme="_theme" />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<HeadContent>
    <style>
        :root {
            --brand-primary:   @_branding.PrimaryColor;
            --brand-secondary: @_branding.SecondaryColor;
            --brand-accent:    @_branding.AccentColor;
        }
    </style>
</HeadContent>

@code {
    private BrandingConfig _branding = default!;
    private MudTheme _theme = default!;

    protected override void OnInitialized()
    {
        _branding = BrandingOptions.Value;
        _theme = new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                // MudBlazor v9: MudColor lives in MudBlazor.Utilities — @using above is required (CS0246 fix)
                Primary            = new MudColor(_branding.PrimaryColor),
                Secondary          = new MudColor(_branding.SecondaryColor),
                AppbarBackground   = new MudColor(_branding.PrimaryColor),
                DrawerBackground   = new MudColor(_branding.SecondaryColor),
                DrawerText         = Colors.Shades.White,
            },
            // MudBlazor v9: Typography slot types (Default, H6, etc.) are short class names that collide
            // with C# identifiers under TreatWarningsAsErrors=true, causing CS0246.
            // The font is enforced via the CSS universal selector in wwwroot/app.css instead — see §19.
        };
    }
}
```

> [!TIP]
> **White-Labeling Strategy: Dynamic Styling Root**
>
> By injecting CSS custom properties into `<head>` via `<HeadContent>`, even third-party components and legacy stylesheets automatically inherit the brand colours. Since this renders server-side, there is no flash of unstyled content (FOUC).

---

## 9. Feature Implementation (The ERP) <a name="feature-implementation"></a>

### `Program.cs` — Composition Root & Middleware Pipeline

```csharp
using ErpPortal.Core.Config;
using ErpPortal.Core.Contracts;
using ErpPortal.Core.Domain;
using ErpPortal.Components;
using ErpPortal.Infrastructure.Http;
using ErpPortal.Infrastructure.Repositories;
using ErpPortal.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
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

app.UseStatusCodePagesWithReExecute("/error/{0}");
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
```

### `Components/App.razor` — Router Root & Enhanced Navigation

Enhanced Navigation is configured here. It intercepts link clicks on `<a>` tags, fetches the new page as an HTML fragment, and patches only the changed DOM nodes — giving users SPA-like transitions with zero client-side router overhead.

```razor
<!DOCTYPE html>
<html lang="en">
@inject IOptions<BrandingConfig> BrandingOptions
@using Microsoft.AspNetCore.Components
@using static Microsoft.AspNetCore.Components.Web.RenderMode

<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta name="robots"    content="noindex, nofollow, noarchive, nosnippet" />
    <meta name="googlebot" content="noindex, nofollow" />
    <base href="/" />
    <link href="https://fonts.googleapis.com/css2?family=Libre+Franklin:ital,wght@0,100..900;1,100..900&display=swap" rel="stylesheet" />
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
    <link rel="stylesheet" href="@Assets[\"app.css\"]" />
    <link rel="stylesheet" href="@Assets[\"ErpPortal.styles.css\"]" />
    <ImportMap />
    <HeadOutlet />
</head>

<body>
    @* Use per-request render mode. Excluded routes (e.g., /login) stay static SSR,
       while authenticated app routes remain interactive. *@
    <Routes @rendermode="PageRenderMode" />
    <script src="@Assets[\"_framework/blazor.web.js\"]"></script>
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>
</body>

</html>

@code {
    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    private IComponentRenderMode? PageRenderMode =>
        HttpContext.AcceptsInteractiveRouting() ? InteractiveServer : null;
}
```

> [!IMPORTANT]
> **Enhanced Navigation: How It Works**
>
> When a user clicks an `<a>` link, Blazor's `blazor.web.js` intercepts the navigation, issues a fetch request for the next page, and diffs only the changed HTML sections into the DOM — no full reload, no flash. This is functionally equivalent to TanStack Router's client-side navigation, but powered by server-rendered HTML. To opt out of Enhanced Navigation for a specific link (e.g., external URLs or file downloads), add `data-enhance-nav="false"` to the anchor tag.

### `Components/RedirectToLogin.razor`

```razor
@inject NavigationManager NavigationManager

@code {
    protected override void OnInitialized()
    {
        NavigationManager.NavigateTo($"/login?returnUrl={Uri.EscapeDataString(NavigationManager.Uri)}", forceLoad: true);
    }
}
```

### `Components/Routes.razor`

```razor
<Router AppAssembly="typeof(Program).Assembly" NotFoundPage="typeof(Pages.NotFound)">
    <Found Context="routeData">
        <AuthorizeRouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)">
            <NotAuthorized>
                <RedirectToLogin />
            </NotAuthorized>
            <Authorizing>
                <MudProgressLinear Indeterminate="true" Color="Color.Primary" />
            </Authorizing>
        </AuthorizeRouteView>
        <FocusOnNavigate RouteData="routeData" Selector="h1" />
    </Found>
</Router>
```

### `_Imports.razor`

```razor
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using static Microsoft.AspNetCore.Components.Web.RenderMode
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.JSInterop
@using ErpPortal
@using ErpPortal.Components
@using MudBlazor
@using ErpPortal.Core.Contracts
@using ErpPortal.Core.Domain
@using ErpPortal.Infrastructure.Services
@using ErpPortal.Core.Config
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Components.Authorization
@using Microsoft.AspNetCore.OutputCaching
@using Microsoft.Extensions.Options
@using System.ComponentModel.DataAnnotations
```

### `Components/Layout/MainLayout.razor`

```razor
@inherits LayoutComponentBase
@inject LayoutService LayoutSvc
@inject IOptions<BrandingConfig> BrandingOptions
@inject NavigationManager Nav
@implements IDisposable

<ThemeProvider />

<PageTitle>@_branding.CompanyName</PageTitle>

<MudLayout>
    <MudAppBar Elevation="1" Color="Color.Primary">
        <MudIconButton Icon="@Icons.Material.Filled.Menu"
                       Color="Color.Inherit"
                       Edge="Edge.Start"
                       OnClick="@(() => LayoutSvc.ToggleSidebar())" />
        <MudText Typo="Typo.h6" Class="ml-3">@_branding.CompanyName</MudText>
    </MudAppBar>

    <MudDrawer @bind-Open="@LayoutSvc.IsSidebarOpen" Elevation="2" Variant="@DrawerVariant.Responsive">
        <NavMenu />
    </MudDrawer>

    <MudMainContent>
        <MudContainer MaxWidth="MaxWidth.ExtraLarge" Class="pt-4">
            @Body
        </MudContainer>
    </MudMainContent>
</MudLayout>

@code {
    private BrandingConfig _branding = default!;

    protected override void OnInitialized()
    {
        _branding = BrandingOptions.Value;
        // Subscribe to LayoutService changes — equivalent of useObservable(layoutService)
        LayoutSvc.OnChange += StateHasChanged;
        // Auto-close mobile drawer on navigation — equivalent of the useEffect LocationChanged handler
        Nav.LocationChanged += OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        => LayoutSvc.CloseSidebar();

    public void Dispose()
    {
        LayoutSvc.OnChange -= StateHasChanged;
        Nav.LocationChanged -= OnLocationChanged;
    }
}
```

### `Components/Layout/NavMenu.razor`

```razor
<MudNavMenu>
    <MudText Typo="Typo.overline" Class="px-4 mt-2 mud-text-secondary">MAIN MENU</MudText>
    <MudNavLink Href="/dashboard" Icon="@Icons.Material.Filled.Dashboard" Match="NavLinkMatch.All">Dashboard</MudNavLink>
    <MudNavLink Href="/users"     Icon="@Icons.Material.Filled.People">Users</MudNavLink>
    <MudNavLink Href="/tasks"     Icon="@Icons.Material.Filled.Assignment">Tasks</MudNavLink>
</MudNavMenu>
```

### Index Redirect (`Components/Pages/Index.razor`)

```razor
@page "/"
@inject NavigationManager Nav

@code {
    protected override void OnInitialized()
        => Nav.NavigateTo("/dashboard", replace: true);
}
```

### Login Page (`Components/Pages/Login.razor`)

> [!NOTE]
> **Cookie Login Should Use Plain HTTP POST**
>
> The login page is intentionally excluded from interactive routing and posts to a controller endpoint.
> This ensures cookie headers are written before an interactive circuit is active and keeps antiforgery validation reliable.

> [!NOTE] **DummyJSON Login Credentials**
>
> Use any credentials from [dummyjson.com/users](https://dummyjson.com/users). Example: username `emilys`, password `emilyspass`.

```razor
@page "/login"
@attribute [ExcludeFromInteractiveRouting]
@* @using Microsoft.AspNetCore.Antiforgery *@
@using Microsoft.Extensions.Options
@* @inject IAntiforgery Antiforgery *@
@inject IOptions<BrandingConfig> BrandingOptions
@layout ErpPortal.Components.Layout.MainLayout

<PageTitle>Sign In — @BrandingOptions.Value.CompanyName</PageTitle>

<form method="post" action="/account/login">
    @* <input type="hidden" name="__RequestVerificationToken" value="@AntiforgeryToken" /> *@
    <AntiforgeryToken />

    @* Mud inputs still submit standard form field names for controller model binding *@
    <MudTextField @bind-Value="Model.Username"
                  UserAttributes="@(new Dictionary<string, object>
                  {
                      ["name"] = "Username",
                      ["autocomplete"] = "username"
                  })" />

    <MudTextField @bind-Value="Model.Password"
                  InputType="InputType.Password"
                  UserAttributes="@(new Dictionary<string, object>
                  {
                      ["name"] = "Password",
                      ["autocomplete"] = "current-password"
                  })" />

    <MudCheckBox @bind-Value="Model.RememberMe"
                 UserAttributes="@(new Dictionary<string, object>
                 {
                     ["name"] = "RememberMe",
                     ["value"] = "true"
                 })" />

    <MudButton ButtonType="ButtonType.Submit">Sign In</MudButton>
</form>

@code {
    // [CascadingParameter]
    // private HttpContext? HttpContext { get; set; }
    d
    [SupplyParameterFromQuery(Name = "error")]
    private string? Error { get; set; }

    private LoginModel Model { get; set; } = new();
    private string? _errorMessage;
    // private string? AntiforgeryToken =>
    //     HttpContext is null
    //        ? null
    //        : Antiforgery.GetAndStoreTokens(HttpContext).RequestToken;

    protected override void OnParametersSet()
        => _errorMessage = Error switch
        {
            "invalid" => "Please provide both username and password.",
            "blocked" => "Login request was blocked by the upstream service.",
            "unreachable" => "Could not reach the authentication service.",
            "failed" => "Authentication failed. Please check your credentials.",
            _ => null,
        };

    private sealed class LoginModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }
}
```

### Account Controller (`Controllers/AccountController.cs`) <a name="account-controller"></a>

Controller endpoints are the write-path for authentication. This keeps cookie header writes and antiforgery validation on standard HTTP POST requests.

```csharp
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ErpPortal.Core.Contracts;
using ErpPortal.Core.Domain;
using ErpPortal.Core.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace ErpPortal.Controllers;

[Route("account")]
public sealed class AccountController : Controller
{
    private readonly IErpHttpClient _http;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IErpHttpClient http, ILogger<AccountController> logger)
    {
        _http = http;
        _logger = logger;
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromForm] LoginForm model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return Redirect("/login?error=invalid");

        try
        {
            int sessionMinutes = model.RememberMe ? 60 * 24 * 30 : 60;
            User user = await _http.PostAsync<User>(
                "/auth/login",
                new { username = model.Username, password = model.Password },
                ct);

            List<Claim> claims =
            [
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new("FirstName", user.FirstName),
                new("LastName", user.LastName),
                new("Token", user.Token ?? string.Empty),
            ];

            ClaimsIdentity identity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            ClaimsPrincipal principal = new(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe
                        ? DateTimeOffset.UtcNow.AddDays(30)
                        : DateTimeOffset.UtcNow.AddHours(1),
                });

            return Redirect("/dashboard");
        }
        catch (AppException ex)
        {
            string reason = ex.StatusCode switch
            {
                401 => "invalid",
                403 => "blocked",
                0 => "unreachable",
                _ => "failed",
            };

            return Redirect($"/login?error={reason}");
        }
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("User logged out");
        return Redirect("/login");
    }

    public sealed class LoginForm
    {
        [Required] public string Username { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }
}
```

### Dashboard (`Components/Pages/Dashboard.razor`)

```razor
@page "/dashboard"
@rendermode InteractiveServer
@attribute [Authorize]
@inject LayoutService LayoutSvc
@inject IOptions<BrandingConfig> BrandingOptions

<PageTitle>Dashboard — @BrandingOptions.Value.CompanyName</PageTitle>
<HeadContent>
    <meta name="description" content="ERP Dashboard Overview." />
</HeadContent>

<MudText Typo="Typo.h4" Class="mb-4">Dashboard Overview</MudText>

<MudGrid>
    <MudItem xs="12" sm="6" lg="4">
        <MudPaper Elevation="2" Class="pa-4">
            <MudStack Row="true" AlignItems="AlignItems.Center">
                <MudIcon Icon="@Icons.Material.Filled.People" Color="Color.Primary" Size="Size.Large" />
                <MudStack>
                    <MudText Typo="Typo.caption" Color="Color.Secondary">Total Users (Server State)</MudText>
                    <MudText Typo="Typo.h5" Class="font-weight-bold">150</MudText>
                </MudStack>
            </MudStack>
        </MudPaper>
    </MudItem>

    <MudItem xs="12" sm="6" lg="4">
        <MudPaper Elevation="2" Class="pa-4">
            <MudStack Row="true" AlignItems="AlignItems.Center">
                <MudIcon Icon="@Icons.Material.Filled.Menu" Color="Color.Info" Size="Size.Large" />
                <MudStack>
                    <MudText Typo="Typo.caption" Color="Color.Secondary">Sidebar (UI State)</MudText>
                    <MudChip T="string" Color="@(LayoutSvc.IsSidebarOpen ? Color.Success : Color.Default)" Size="Size.Small">
                        @(LayoutSvc.IsSidebarOpen ? "Expanded" : "Collapsed")
                    </MudChip>
                </MudStack>
            </MudStack>
        </MudPaper>
    </MudItem>

    <MudItem xs="12" sm="12" lg="4">
        <MudPaper Elevation="2" Class="pa-4" Style="border: 1px dashed var(--brand-primary)">
            <MudText Typo="Typo.subtitle2" Class="mb-2">Pro Insight: Dynamic Theming</MudText>
            <MudText Typo="Typo.body2">
                This card's border uses a <strong>CSS Variable</strong> synced via our
                ThemeProvider for brand-consistent styling across all components.
            </MudText>
        </MudPaper>
    </MudItem>
</MudGrid>
```

### Users CRUD with MudTable (`Components/Pages/Users/Index.razor`)

> [!NOTE]
> **`MudTable<T>` instead of `QuickGrid<T>`**
>
> MudBlazor v9 and `Microsoft.AspNetCore.Components.QuickGrid` both export a component named `TemplateColumn`. Razor cannot disambiguate them when both namespaces appear in `_Imports.razor` (`RZ9985`). `MudTable<T>` is already available via `MudBlazor` and has no naming collisions, so it is used here instead.

```razor
@page "/users"
@rendermode InteractiveServer
@attribute [Authorize]
@attribute [OutputCache(PolicyName = "UsersList")]
@inject IRepository<User> UserRepo
@inject INotificationService Notifier

<PageTitle>User Management</PageTitle>

<MudStack Row="true" Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center" Class="mb-4">
    <MudText Typo="Typo.h4">User Management</MudText>
    <MudButton Variant="Variant.Filled" Color="Color.Primary"
               OnClick="@(() => _createModalOpen = true)">
        Add User
    </MudButton>
</MudStack>

@if (_isLoading)
{
    <MudSkeleton Height="400px" />
}
else if (_error is not null)
{
    <MudAlert Severity="Severity.Error">@_error</MudAlert>
}
else
{
    <MudPaper Elevation="2" Class="pa-2">
        <MudTextField @bind-Value="_globalFilter"
                      Placeholder="Search users..."
                      Adornment="Adornment.Start"
                      AdornmentIcon="@Icons.Material.Filled.Search"
                      Class="mb-3" />

        @* MudBlazor v9: MudTable replaces QuickGrid — both libraries export TemplateColumn and
           Razor cannot disambiguate them (RZ9985). MudTable carries no such ambiguity. *@
        <MudTable Items="@FilteredUsers" RowsPerPage="20" Hover="true" Striped="true">
            <HeaderContent>
                <MudTh>Name</MudTh>
                <MudTh>Email</MudTh>
                <MudTh>Username</MudTh>
                <MudTh></MudTh>
            </HeaderContent>
            <RowTemplate>
                <MudTd>
                    <MudStack Row="true" AlignItems="AlignItems.Center">
                        @* MudBlazor v9: MUD0002 rejects Image attribute on MudAvatar — use initials *@
                        <MudAvatar Size="Size.Small" Color="Color.Primary">
                            @context.FirstName[0]@context.LastName[0]
                        </MudAvatar>
                        <MudText Typo="Typo.body2">@context.FirstName @context.LastName</MudText>
                    </MudStack>
                </MudTd>
                <MudTd>@context.Email</MudTd>
                <MudTd>@context.Username</MudTd>
                <MudTd>
                    <MudStack Row="true" Justify="Justify.FlexEnd">
                        <MudIconButton Icon="@Icons.Material.Filled.Edit"
                                       Size="Size.Small" Color="Color.Default" />
                        <MudIconButton Icon="@Icons.Material.Filled.Delete"
                                       Size="Size.Small" Color="Color.Error"
                                       OnClick="@(() => OpenDeleteConfirm(context))" />
                    </MudStack>
                </MudTd>
            </RowTemplate>
            <PagerContent>
                <MudTablePager />
            </PagerContent>
        </MudTable>
    </MudPaper>
}

@* Create User Modal *@
<MudDialog @bind-Visible="_createModalOpen" Options="_dialogOptions">
    <TitleContent><MudText Typo="Typo.h6">Add New User</MudText></TitleContent>
    <DialogContent>
        <EditForm Model="_newUser" OnValidSubmit="HandleCreateUserAsync" FormName="CreateUserForm">
            <DataAnnotationsValidator />
            <MudStack>
                <MudTextField @bind-Value="_newUser.FirstName" Label="First Name" For="@(() => _newUser.FirstName)" />
                <MudTextField @bind-Value="_newUser.LastName"  Label="Last Name"  For="@(() => _newUser.LastName)" />
                <MudTextField @bind-Value="_newUser.Email"     Label="Email"      For="@(() => _newUser.Email)" />
                <MudTextField @bind-Value="_newUser.Username"  Label="Username"   For="@(() => _newUser.Username)" />
                <MudButton ButtonType="ButtonType.Submit" Variant="Variant.Filled"
                           Color="Color.Primary" Disabled="_isSubmitting">
                    Create User
                </MudButton>
            </MudStack>
        </EditForm>
    </DialogContent>
</MudDialog>

@* Delete Confirmation Dialog — equivalent of modals.openConfirmModal() *@
<MudDialog @bind-Visible="_deleteConfirmOpen" Options="_dialogOptions">
    <TitleContent><MudText Typo="Typo.h6">Confirm Deletion</MudText></TitleContent>
    <DialogContent>
        <MudText>This action cannot be undone.</MudText>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="@(() => _deleteConfirmOpen = false)">Cancel</MudButton>
        <MudButton Color="Color.Error" Variant="Variant.Filled"
                   OnClick="HandleDeleteAsync" Disabled="_isDeleting">
            Delete User
        </MudButton>
    </DialogActions>
</MudDialog>

@code {
    private IReadOnlyList<User> _users = [];
    private bool _isLoading, _isSubmitting, _isDeleting, _createModalOpen, _deleteConfirmOpen;
    private string? _error;
    private string _globalFilter = string.Empty;
    private User? _userToDelete;

    private readonly DialogOptions _dialogOptions = new() { CloseOnEscapeKey = true };
    private readonly CreateUserModel _newUser = new();

    private IEnumerable<User> FilteredUsers => _users
        .Where(u => string.IsNullOrEmpty(_globalFilter) ||
                    u.FirstName.Contains(_globalFilter, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(_globalFilter, StringComparison.OrdinalIgnoreCase) ||
                    u.Username.Contains(_globalFilter, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        try
        {
            (IReadOnlyList<User> data, int _) = await UserRepo.GetAllAsync(0, 50);
            _users = data;
        }
        catch (Exception e) { _error = e.Message; }
        finally { _isLoading = false; }
    }

    private void OpenDeleteConfirm(User user)
    {
        _userToDelete = user;
        _deleteConfirmOpen = true;
    }

    private async Task HandleDeleteAsync()
    {
        if (_userToDelete is null) return;
        _isDeleting = true;
        try
        {
            await UserRepo.DeleteAsync(_userToDelete.Id);
            _users = _users.Where(u => u.Id != _userToDelete.Id).ToList();
            _deleteConfirmOpen = false;
        }
        finally { _isDeleting = false; }
    }

    private async Task HandleCreateUserAsync()
    {
        _isSubmitting = true;
        try
        {
            User user = new User(0, _newUser.Username, _newUser.Email, _newUser.FirstName, _newUser.LastName, string.Empty);
            User created = await UserRepo.CreateAsync(user);
            _users = [.. _users, created];
            _createModalOpen = false;
        }
        finally { _isSubmitting = false; }
    }

    private sealed class CreateUserModel
    {
        [Required] public string FirstName { get; set; } = string.Empty;
        [Required] public string LastName  { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email    { get; set; } = string.Empty;
        [Required] public string Username  { get; set; } = string.Empty;
    }
}
```

### Tasks with Virtualize (`Components/Pages/Tasks/Index.razor`)

> [!NOTE]
> **`<Virtualize<T>>`: The Built-in Windowed Renderer**
>
> Blazor's `<Virtualize>` component renders only the visible items in a large list — the exact equivalent of `@tanstack/react-virtual`'s `useVirtualizer`. It ships with the framework; no extra package required. Set `ItemSize` to your estimated row height and `OverscanCount` to control the buffer above/below the viewport.

```razor
@page "/tasks"
@rendermode InteractiveServer
@attribute [Authorize]
@attribute [OutputCache(PolicyName = "TodosList")]
@inject IRepository<Todo> TodoRepo

<PageTitle>Task Management</PageTitle>

<MudStack Row="true" Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center" Class="mb-4">
    <MudStack Row="true" AlignItems="AlignItems.Baseline">
        <MudText Typo="Typo.h4">Task Management</MudText>
        @if (_todos is not null)
        {
            <MudText Typo="Typo.caption" Color="Color.Secondary">
                (@_todos.Count tasks — virtualized)
            </MudText>
        }
    </MudStack>
    <MudButton Variant="Variant.Filled" Color="Color.Primary"
               StartIcon="@Icons.Material.Filled.Add">Add Task</MudButton>
</MudStack>

@if (_isLoading)
{
    <MudSkeleton Height="600px" />
}
else if (_error is not null)
{
    <MudAlert Severity="Severity.Error">@_error</MudAlert>
}
else
{
    @* Scrollable container for the virtualizer *@
    <MudPaper Elevation="2" Style="height:600px;overflow-y:auto;">
        @* <Virtualize> renders only the visible rows into the DOM.
           Equivalent of rowVirtualizer.getVirtualItems() — no third-party package required. *@
        <Virtualize Items="_todos" Context="todo" ItemSize="55" OverscanCount="10">
            <div style="display:flex;align-items:center;padding:0 16px;height:55px;
                        border-bottom:1px solid var(--mud-palette-divider);">
                <MudText Typo="Typo.body2" Style="flex:1"
                         Class="@(todo.Completed ? "mud-text-disabled" : "")"
                         Style="@(todo.Completed ? "text-decoration:line-through" : "")">
                    @todo.TodoText
                </MudText>

                <MudChip T="string" Color="@(todo.Completed ? Color.Success : Color.Default)"
                         Size="Size.Small"
                         OnClick="@(() => HandleToggleAsync(todo))"
                         Class="mr-3">
                    @(todo.Completed ? "Completed" : "Pending")
                </MudChip>

                <MudIconButton Icon="@Icons.Material.Filled.Delete"
                               Color="Color.Error"
                               Size="Size.Small"
                               OnClick="@(() => OpenDeleteConfirm(todo))" />
            </div>
        </Virtualize>
    </MudPaper>
}

@* Delete confirmation dialog *@
<MudDialog @bind-Visible="_deleteConfirmOpen">
    <TitleContent><MudText Typo="Typo.h6">Delete Task</MudText></TitleContent>
    <DialogContent>
        <MudText>Are you sure you want to delete this task?</MudText>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="@(() => _deleteConfirmOpen = false)">Cancel</MudButton>
        <MudButton Color="Color.Error" Variant="Variant.Filled" OnClick="HandleDeleteAsync">Delete</MudButton>
    </DialogActions>
</MudDialog>

@code {
    private List<Todo> _todos = [];
    private bool _isLoading, _deleteConfirmOpen;
    private string? _error;
    private Todo? _todoToDelete;

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        try
        {
            (IReadOnlyList<Todo> data, int _) = await TodoRepo.GetAllAsync(0, 150);
            _todos = [.. data];
        }
        catch (Exception e) { _error = e.Message; }
        finally { _isLoading = false; }
    }

    private async Task HandleToggleAsync(Todo todo)
    {
        Todo updated = await TodoRepo.UpdateAsync(todo.Id, todo with { Completed = !todo.Completed });
        int index   = _todos.FindIndex(t => t.Id == todo.Id);
        if (index >= 0) _todos[index] = updated;
    }

    private void OpenDeleteConfirm(Todo todo)
    {
        _todoToDelete = todo;
        _deleteConfirmOpen = true;
    }

    private async Task HandleDeleteAsync()
    {
        if (_todoToDelete is null) return;
        await TodoRepo.DeleteAsync(_todoToDelete.Id);
        _todos.RemoveAll(t => t.Id == _todoToDelete.Id);
        _deleteConfirmOpen = false;
    }
}
```

> [!TIP]
> **State Management Practice: Server vs UI State**
>
> Always keep "Server State" (data loaded via `IRepository<T>`) separate from "UI State" (`LayoutService`, component fields). Use `[OutputCache]` on pages for server-state caching and `StateHasChanged()` for UI-state reactivity. Never cache API data in a `LayoutService`; let the output cache handle invalidation.

---

## 10. State Management (Reactive Services) <a name="state-management"></a>

The reactive state pattern maps cleanly to Blazor's component model. Here is a summary of all pieces working together:

| File | Purpose |
|---|---|
| `Infrastructure/Services/LayoutService.cs` | Manages `{ IsSidebarOpen }` with an `event Action? OnChange` |
| `Components/Layout/MainLayout.razor` | Subscribes to `OnChange` and calls `StateHasChanged()` in `OnInitialized` |
| `Components/Pages/Dashboard.razor` | Reads `LayoutSvc.IsSidebarOpen` directly (no wrapper hook needed) |

To add a new piece of UI state (e.g., a notification drawer), create a new scoped service with an `OnChange` event, register it in `Program.cs`, and subscribe from the consuming component. No external state library (Redux, Zustand, MobX) is needed — Blazor's component lifecycle is the state container.

---

## 11. Containerization (Podman/Docker) <a name="containerization"></a>

Because this is an ASP.NET Core application, the runtime is the container itself — we use Microsoft's official `aspnet` base image. No separate reverse proxy is needed for basic hosting.

> [!TIP]
> **Production Strategy: ASP.NET Core as the Server**
>
> Unlike the SPA version which required Nginx to serve static files, Blazor SSR serves everything — pages, API proxying, and static assets — from a single `dotnet` process. The container is simpler, startup is faster, and you get the full Kestrel performance profile out of the box.

### `Containerfile`

```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /app

# Restore packages first (layer-cached until .csproj changes)
COPY *.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o ./publish --no-restore

# Stage 2: Runtime (much smaller than SDK image)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runner
WORKDIR /app

# Non-root user for security
RUN adduser --disabled-password --no-create-home appuser
USER appuser

COPY --from=builder /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "ErpPortal.dll"]
```

### `.containerignore`

```text
bin/
obj/
.git/
.vscode/
*.md
Containerfile
.containerignore
appsettings.Development.json
```

> [!CAUTION]
> **Runtime Environment Variables**
>
> Unlike Vite where secrets are baked into the JS bundle at build time, ASP.NET Core reads environment variables at **runtime**. Pass secrets as container environment variables — never bake them into the image. This is strictly more secure.

---

## 12. Running the Application <a name="running"></a>

### Local Development

#### Step 1: Trust the ASP.NET Core Dev Certificate

Before running any project over HTTPS, ensure your machine trusts the .NET self-signed development certificate. Without this step, browsers will show a "Your connection is not private" warning and `HttpClient` calls between the API gateway and the Blazor portal will fail with SSL errors.

```bash
# Generate the certificate (idempotent — safe to run multiple times)
dotnet dev-certs https

# Trust the certificate (required once per machine — OS will prompt for confirmation)
dotnet dev-certs https --trust
```

> [!NOTE]
> **One-Time Setup**
>
> You only need to run `dotnet dev-certs https --trust` once per machine. The trusted certificate persists across projects and .NET SDK upgrades. If you reinstall the SDK or rotate the certificate, re-run the command.

#### Step 2: Start the Dev Server with `dotnet watch`

`dotnet watch` (shorthand for `dotnet watch run`) starts the application and watches for file changes. When you edit a `.razor` or `.cs` file, it hot-reloads the change — comparable to Vite's HMR. For Blazor pages, changes to markup and `@code` blocks are applied without a full restart.

```bash
# Restore dependencies
dotnet restore

# Start the dev server with Hot Reload (reads URL from launchSettings.json)
dotnet watch run
# → Listening on https://localhost:5001 (or http://localhost:5000)
```

If the app defaults to HTTP, force the HTTPS profile defined in `launchSettings.json`:

```bash
dotnet watch run --launch-profile https
```

To override everything and bind to a specific HTTPS port directly:

```bash
dotnet watch run --urls "https://localhost:7001"
```

> [!TIP]
> **`dotnet watch` = `npm run dev`**
>
> `dotnet watch run` starts the application and watches for file changes. When you edit a `.razor` or `.cs` file, it hot-reloads the change — comparable to Vite's HMR. For Blazor pages, changes to markup and `@code` blocks are applied without a full restart.

#### Step 3: Verify Project Configuration

For HTTPS to work correctly during the `watch` process, your `launchSettings.json` and `Program.cs` should be configured correctly.

**`Properties/launchSettings.json`** — Ensure an `https` profile exists with a valid SSL port (7000+ range by convention):

```json
"profiles": {
  "https": {
    "commandName": "Project",
    "dotnetRunMessages": true,
    "launchBrowser": true,
    "applicationUrl": "https://localhost:7001;http://localhost:5001",
    "environmentVariables": {
      "ASPNETCORE_ENVIRONMENT": "Development"
    }
  }
}
```

**`Program.cs`** — Ensure the HTTPS redirection middleware is active:

```csharp
app.UseHttpsRedirection(); // Forces HTTP requests to HTTPS
```

This is already present in the ErpPortal `Program.cs` (see Section 9, line `app.UseHttpsRedirection()`).

#### Quick Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| Browser shows "Your connection is not private" | Dev certificate not trusted | Run `dotnet dev-certs https --trust` |
| Browser sticks to old port after config change | Browser cache | Try an Incognito/Private window |
| `dotnet watch` inside Docker fails HTTPS | Host cert not mounted | Export the certificate and mount it as a volume into the container |
| HSTS prevents page load in dev | `app.UseHsts()` active in Development | Only enable HSTS in Production (the ErpPortal template already gates this correctly) |

### Running Both Projects Simultaneously (HTTPS Debug)

Both `ErpPortal.Api` (port 7002) and `ErpPortal` (port 7001) must run at the same time for the full application to work. The API must start before the Blazor app makes its first request.

#### Option A: VS Code — Single F5 Launch (Recommended)

The compound launch configuration in `.vscode/launch.json` (section 4.1) starts both projects with HTTPS in one keystroke. Update the `env` block to use `https`:

```json
// .vscode/launch.json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "API Gateway (HTTPS)",
      "type": "dotnet",
      "request": "launch",
      "projectPath": "${workspaceFolder}/ErpPortal.Api/ErpPortal.Api.csproj",
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_URLS": "https://localhost:7002;http://localhost:5002"
      }
    },
    {
      "name": "Blazor Portal (HTTPS)",
      "type": "dotnet",
      "request": "launch",
      "projectPath": "${workspaceFolder}/ErpPortal/ErpPortal.csproj",
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_URLS": "https://localhost:7001;http://localhost:5001"
      }
    }
  ],
  "compounds": [
    {
      "name": "▶ Full ERP Solution (HTTPS)",
      "configurations": ["API Gateway (HTTPS)", "Blazor Portal (HTTPS)"],
      "stopAll": true
    }
  ]
}
```

Press **F5** and select **▶ Full ERP Solution (HTTPS)** — VS Code launches both processes, attaches a debugger to each, and lets you set breakpoints in either project simultaneously.

> [!NOTE]
> The `ErpPortal` `appsettings.Development.json` should point to the API's HTTPS port:
> ```json
> { "ApiSettings": { "BaseUrl": "https://localhost:7002/api" } }
> ```

#### Option B: Two PowerShell Terminals (no launch file needed)

Open two separate terminals and run one command in each. The `--urls` flag overrides any `launchSettings.json` — no profile required:

```powershell
# Terminal 1 — API Gateway
cd ErpPortal.Api
dotnet watch run --urls "https://localhost:7002;http://localhost:5002"
# → Now listening on: https://localhost:7002
```

```powershell
# Terminal 2 — Blazor Portal (start after Terminal 1 is ready)
cd ErpPortal
dotnet watch run --urls "https://localhost:7001;http://localhost:5001"
# → Now listening on: https://localhost:7001
```

> [!TIP]
> `dotnet watch run` gives you Hot Reload on both projects. API changes restart the gateway; Blazor markup changes update without a restart.

#### Option C: Single PowerShell Script (no launch file needed)

Save as `run-dev.ps1` in the solution root and run it from one terminal. URLs are passed directly — no `launchSettings.json` profile required:

```powershell
#!/usr/bin/env pwsh
# run-dev.ps1 — Starts API Gateway and Blazor Portal in parallel (HTTPS)
# No launchSettings.json profile needed — URLs are supplied inline.

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

$apiJob = Start-Job -ScriptBlock {
    Set-Location "$using:root/ErpPortal.Api"
    dotnet watch run --urls "https://localhost:7002;http://localhost:5002"
} -Name "API"

$appJob = Start-Job -ScriptBlock {
    # Give the API 3 seconds to bind its port before the app starts
    Start-Sleep -Seconds 3
    Set-Location "$using:root/ErpPortal"
    dotnet watch run --urls "https://localhost:7001;http://localhost:5001"
} -Name "App"

Write-Host "API Gateway   → https://localhost:7002" -ForegroundColor Cyan
Write-Host "Blazor Portal → https://localhost:7001" -ForegroundColor Cyan
Write-Host "Press Ctrl+C to stop both..." -ForegroundColor Yellow

try {
    while ($true) {
        Receive-Job -Job $apiJob, $appJob
        Start-Sleep -Milliseconds 200
    }
}
finally {
    Stop-Job  -Job $apiJob, $appJob
    Remove-Job -Job $apiJob, $appJob -Force
    Write-Host "Both processes stopped." -ForegroundColor Green
}
```

Run it:

```powershell
.\run-dev.ps1
```

> [!NOTE]
> **No `launchSettings.json` required**
>
> Options B and C pass `--urls` directly to the `dotnet` CLI. This overrides any value in `launchSettings.json` and works even if the file does not exist. The `ASPNETCORE_ENVIRONMENT` environment variable defaults to `Production` when not set; set it explicitly in the terminal if needed:
> ```powershell
> $env:ASPNETCORE_ENVIRONMENT = "Development"
> dotnet watch run --urls "https://localhost:7002;http://localhost:5002"
> ```

> [!CAUTION]
> **Dev Certificate**
>
> HTTPS on localhost requires the ASP.NET Core dev certificate to be trusted. Run this once on a new machine:
> ```powershell
> dotnet dev-certs https --trust
> ```
> You will be prompted to confirm the certificate trust in your OS. Without this, browsers will show a security warning and `HttpClient` calls between the two projects will fail with SSL errors.

```bash
dotnet publish -c Release -o ./publish
# Output: publish/ directory (self-contained .NET application)
```

### Podman Deployment

```bash
# Build the container image
podman build -t enterprise-erp-portal .

# Run the container — pass configuration as environment variables at RUNTIME
podman run -d \
  -p 8080:8080 \
  --name erp_portal \
  -e "ApiSettings__BaseUrl=https://your-api.example.com" \
  -e "Branding__CompanyName=My Corp ERP" \
  -e "ASPNETCORE_ENVIRONMENT=Production" \
  enterprise-erp-portal

# Verify
podman ps

# View logs
podman logs -f erp_portal
```

Visit `http://localhost:8080` to see your containerized ERP portal.

---

## 13. References & Documentation <a name="references"></a>

### Core Frameworks

- **[ASP.NET Core Blazor](https://learn.microsoft.com/aspnet/core/blazor)**: Microsoft's official Blazor documentation.
- **[Blazor Static SSR](https://learn.microsoft.com/aspnet/core/blazor/components/render-modes)**: Render modes and Enhanced Navigation.
- **[QuickGrid](https://aspnet.github.io/quickgridsamples/)**: Official QuickGrid samples and API reference.
- **[Blazor `<Virtualize>`](https://learn.microsoft.com/aspnet/core/blazor/components/virtualization)**: Built-in list virtualization docs.

### UI & Styling

- **[MudBlazor](https://mudblazor.com)**: Feature-rich Blazor component library (Mantine equivalent).
- **[MudBlazor Theming](https://mudblazor.com/customization/overview)**: Custom theme configuration guide.

### Infrastructure & Tools

- **[DummyJSON](https://dummyjson.com)**: Fake REST API for prototyping.
- **[Podman](https://podman.io)**: Daemonless OCI container manager.
- **[Microsoft.Extensions.Http](https://learn.microsoft.com/dotnet/core/extensions/httpclient-factory)**: Typed HttpClient factory and `DelegatingHandler` patterns.
- **[FluentValidation](https://docs.fluentvalidation.net)**: Powerful validation library for complex form rules.
- **[xUnit](https://xunit.net)**: .NET unit testing framework.
- **[Moq](https://github.com/moq/moq4)**: Mocking framework for .NET.

---

## 14. Appendix: Unit Testing <a name="testing"></a>

Following the **Testability** principle, every concrete service codes against an abstraction, making mock injection trivial. The `xUnit` + `Moq` stack is the .NET equivalent of Vitest + `vi.fn()`.

### Create the Test Project

```bash
dotnet new xunit -n ErpPortal.Tests
cd ErpPortal.Tests
dotnet add reference ../ErpPortal/ErpPortal.csproj
dotnet add package Moq
dotnet add package FluentAssertions
dotnet add package Microsoft.Extensions.Logging.Abstractions
```

### `Tests/UserRepositoryTests.cs`

```csharp
using ErpPortal.Core.Contracts;
using ErpPortal.Core.Domain;
using ErpPortal.Infrastructure.Http;
using ErpPortal.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ErpPortal.Tests;

public sealed class UserRepositoryTests
{
    // Equivalent of beforeEach(() => { mockHttp = { get: vi.fn() ... } })
    private readonly Mock<IErpHttpClient>      _mockHttp     = new();
    private readonly Mock<INotificationService> _mockNotifier = new();
    private readonly NullLogger<UserRepository> _logger       = new();

    private UserRepository CreateRepo()
        => new(_mockHttp.Object, _logger, _mockNotifier.Object);

    [Fact]
    public async Task GetAllAsync_ShouldReturnFormattedUsers()
    {
        // Arrange — equivalent of mockHttp.get.mockResolvedValue(...)
        List<User> users = new List<User>
        {
            new(1, "johnd", "john@example.com", "John", "Doe", "https://example.com/img.jpg")
        };
        _mockHttp
            .Setup(h => h.GetAsync<UsersApiResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsersApiResponse(users, 1));

        UserRepository repo = CreateRepo();

        // Act
        (IReadOnlyList<User> data, int total) = await repo.GetAllAsync(0, 10);

        // Assert — FluentAssertions replaces Vitest's expect().toBe()
        data[0].FirstName.Should().Be("John");
        total.Should().Be(1);
        _mockHttp.Verify(h => h.GetAsync<UsersApiResponse>(
            It.Is<string>(u => u.Contains("limit=10") && u.Contains("skip=0")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldLogCriticalAndThrowOnFailure()
    {
        // Arrange — equivalent of mockHttp.get.mockRejectedValue(...)
        _mockHttp
            .Setup(h => h.GetAsync<UsersApiResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network Error"));

        UserRepository repo = CreateRepo();

        // Act & Assert
        await repo.Invoking(r => r.GetAllAsync())
            .Should().ThrowAsync<HttpRequestException>();
    }
}
```

### Running Tests

```bash
# Run all tests with detailed output (equivalent of npm run test:unit -- --reporter=verbose)
dotnet test --verbosity normal

# Run a single test file
dotnet test --filter "FullyQualifiedName~UserRepositoryTests"

# Run in watch mode (equivalent of npm run test:watch)
dotnet watch test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

> [!NOTE]
> **Test Isolation via DI**
>
> Because all dependencies are injected via interfaces, every test creates fresh `Mock<T>()` objects. Tests are fully isolated — no shared state, no order dependencies. If a test fails, the fault is in the code under test, not in test setup leakage.

---

## 15. Enterprise CI/CD Pipeline (GitHub Actions) <a name="cicd"></a>

### `.github/workflows/main.yml`

```yaml
name: Enterprise Build & Test

on:
  push:
    branches: [ "main" ]
  pull_request:
    branches: [ "main" ]

jobs:
  quality-gate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      # Equivalent of npm ci — restores packages with lock file
      - name: Restore Dependencies
        run: dotnet restore

      # Equivalent of npx tsc --noEmit — compile-time type checking with -warnaserror
      - name: Build (Strict — Warnings as Errors)
        run: dotnet build --no-restore -warnaserror

      # Equivalent of npm run test:unit
      - name: Unit Tests (xUnit)
        run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"

      # Equivalent of npm run build
      - name: Publish Release Artifact
        run: dotnet publish -c Release -o ./publish
        env:
          # Runtime config is passed here — NOT baked into the build
          ASPNETCORE_ENVIRONMENT: Production

  container-delivery:
    needs: quality-gate
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    environment: production
    steps:
      - uses: actions/checkout@v4

      - name: Build and Push Container
        uses: docker/build-push-action@v5
        with:
          context: .
          file: ./Containerfile
          push: true
          tags: ghcr.io/${{ github.repository }}:latest
```

### Azure DevOps (`azure-pipelines-dev.yaml`)

For teams using Azure DevOps, this pipeline restores, builds, and publishes `ErpPortal` as a zipped artifact from the dev branch trigger.

```yaml
# ASP.NET Core
# Build and test ASP.NET Core projects.
# https://docs.microsoft.com/azure/devops/pipelines/ecosystems/dotnet-core

trigger:
- dev/sprint_280/tm/sprint_support/47798

pool:
    vmImage: 'windows-latest'

variables:
    project: 'ErpPortal/ErpPortal.csproj'
    buildPlatform: 'Any CPU'
    buildConfiguration: 'Release'

steps:
- task: UseDotNet@2
    displayName: 'Install .NET SDK'
    inputs:
        packageType: 'sdk'
        useGlobalJson: false
        version: '10.x'

- task: DotNetCoreCLI@2
    displayName: 'Restore NuGet Packages'
    inputs:
        command: restore
        projects: '$(project)'

- task: DotNetCoreCLI@2
    displayName: 'Build ErpPortal'
    inputs:
        command: build
        projects: '$(project)'
        arguments: '--configuration $(buildConfiguration) --no-restore'

- task: DotNetCoreCLI@2
    displayName: 'Publish ErpPortal'
    inputs:
        command: publish
        projects: '$(project)'
        arguments: '--configuration $(buildConfiguration) --no-build --output $(Build.ArtifactStagingDirectory)'
        publishWebProjects: false
        zipAfterPublish: true

- task: PublishBuildArtifacts@1
    inputs:
        PathtoPublish: '$(Build.ArtifactStagingDirectory)'
        ArtifactName: 'Dev_Release_WebUI_Artifact'
        publishLocation: 'Container'
```

> [!IMPORTANT]
> **Runtime Secrets in CI**
>
> Unlike the SPA version where env vars were baked into the JS bundle, ASP.NET Core reads its configuration at **runtime**. Store `ApiSettings__BaseUrl` and other secrets as **GitHub Secrets** or as environment variables on your hosting platform. The container image itself contains zero secrets and is safe to push to a public registry.

---

## 16. Enterprise Privacy & Crawler Shield <a name="privacy-shield"></a>

In enterprise ERP environments, preventing external scraping and protecting internal data is critical. The Blazor SSR approach has a structural advantage here: the application returns `401` / `302 to /login` for unauthenticated requests at the server level, before any HTML is rendered — crawlers never see the content.

### 1. Blocking AI & Social Crawlers (`wwwroot/robots.txt`)

```text
User-agent: *
Disallow: /

# Specifically block AI Scrapers & Social Crawlers
User-agent: GPTBot
User-agent: ChatGPT-User
User-agent: Google-Extended
User-agent: CCBot
User-agent: OAI-SearchBot
User-agent: meta-externalagent
User-agent: Facebot
User-agent: facebookexternalhit
Disallow: /
```

### 2. Global No-Index Meta Tags (`Components/App.razor`)

Inject "No-Index" globally via `<HeadOutlet>` to prevent search engines from indexing the portal. Since these are rendered server-side, they are present in the very first HTTP response — more reliable than injecting them via JavaScript.

```razor
@* In the <head> section of App.razor *@
<meta name="robots"    content="noindex, nofollow, noarchive, nosnippet" />
<meta name="googlebot" content="noindex, nofollow" />
<meta property="og:type" content="website" />
```

### 3. Security Headers via ASP.NET Core Middleware

Set security headers at the middleware level — more reliable than HTML meta tags, and applied before any page logic runs.

```csharp
// In Program.cs — add after app.UseStaticFiles()
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Frame-Options"]        = "DENY";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"]        = "no-referrer";
    // context.Response.Headers["Content-Security-Policy"] =
    //     "default-src 'self'; " +
    //     "script-src 'self'; " +
    //     "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
    //     "font-src 'self' https://fonts.gstatic.com; " +
    //     "img-src 'self' data: https:; " +
    //     "connect-src 'self' https://dummyjson.com;";
    await next();
});
```

### 4. Blocking Crawlers by User-Agent (Middleware)

```csharp
// In Program.cs — add before app.UseRouting()
string[] blockedAgents = new[] { "GPTBot", "ChatGPT", "facebookexternalhit", "meta-externalagent", "CCBot" };

app.Use(async (context, next) =>
{
    string ua = context.Request.Headers.UserAgent.ToString();
    if (blockedAgents.Any(agent => ua.Contains(agent, StringComparison.OrdinalIgnoreCase)))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return;
    }
    await next();
});
```

### 5. Disabling Telemetry — the Zero-Bloat Promise

- **ASP.NET Core**: Zero telemetry collected by default in production builds.
- **MudBlazor**: Pure UI library — no analytics or tracking.
- **Kestrel**: Open source, self-hosted — zero external calls.
- **`dotnet` CLI**: Set `DOTNET_CLI_TELEMETRY_OPTOUT=1` in your container image or CI environment to suppress build-time telemetry entirely.

```dockerfile
# Add to Containerfile — Stage 1 (builder)
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_NOLOGO=1
```

> [!TIP]
> **Security & Privacy Ops: Zero-Trust Scripting**
>
> The primary source of telemetry leakage in ERPs is third-party tracking scripts. In enterprise portals, avoid these entirely. If usage tracking is required, use self-hosted solutions like [Umami](https://umami.is) and integrate them via a strongly-typed `IAnalyticsService` so they can be swapped out via DI.

---

## 17. Hosting on Azure App Service / Fly.io <a name="hosting"></a>

This application is a standard ASP.NET Core app and can be hosted on any platform that supports containers or the .NET runtime.

### Option A: Azure App Service (Recommended for enterprises already on Azure)

#### 1. Publish to Azure Container Registry

```bash
# Tag the image for ACR
podman tag enterprise-erp-portal myregistry.azurecr.io/erp-portal:latest

# Push
podman push myregistry.azurecr.io/erp-portal:latest
```

#### 2. App Service Configuration

In the Azure Portal under **Configuration > Application settings**, add:

| Key | Example Value |
|---|---|
| `ApiSettings__BaseUrl` | `https://api.yourdomain.com` |
| `Branding__CompanyName` | `My Corp ERP` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

> [!IMPORTANT]
> **Double Underscore as Separator**
>
> ASP.NET Core uses `__` (double underscore) to represent nesting in environment variable names. `ApiSettings__BaseUrl` maps to `appsettings.json`'s `ApiSettings.BaseUrl`. This is the .NET equivalent of Netlify's flat environment variable system.

### Option B: Fly.io (Recommended for self-hosted / open-source deployments)

```bash
# Install flyctl
curl -L https://fly.io/install.sh | sh

# Deploy from the Containerfile
fly launch
fly secrets set ApiSettings__BaseUrl="https://api.yourdomain.com"
fly deploy
```

### Client-Side Routing — No `_redirects` File Needed

Unlike the SPA version which required a `public/_redirects` file to redirect all paths to `index.html`, Blazor SSR handles routing on the **server**. Every URL is matched by ASP.NET Core's routing middleware and rendered server-side. There are no client-side routing edge cases on page refresh.

---

## 18. How to Debug <a name="debugging"></a>

Blazor SSR debugging is native to the .NET ecosystem — no source maps configuration needed. VS and VS Code attach to the running process and step through C# and Razor files directly.

### Visual Studio (Windows/macOS) — Recommended

1. Open `ErpPortal.sln`.
2. Press **F5** → VS builds, launches, and attaches the debugger.
3. Set breakpoints in any `.cs`, `.razor`, or code-behind file.
4. The full call stack, locals, watch window, and Immediate Window are available.

### VS Code

Install the **C# Dev Kit** extension, then:

1. Create `.vscode/launch.json`:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Debug Blazor SSR (dotnet)",
      "type": "dotnet",
      "request": "launch",
      "projectPath": "${workspaceFolder}/ErpPortal/ErpPortal.csproj"
    }
  ]
}
```

2. Press **F5** → the debugger attaches to the running Kestrel server.
3. Set breakpoints in `.razor` files — VS Code pauses execution in both the C# `@code` block and the rendering logic.

### Hot Reload

```bash
# Start with Hot Reload
dotnet watch run

# Hot Reload behaviour:
# - Editing .razor markup or @code blocks → UI updates without restart
# - Editing .cs service files → application restarts automatically
# - Editing appsettings.json → configuration reloads at runtime (IOptionsMonitor)
```

### Debugging Common Issues

| Symptom | Cause | Fix |
|---|---|---|
| `NullReferenceException` in `@code` | Nullable not initialised | Enable `<Nullable>enable</Nullable>`; initialise fields in `OnInitialized` |
| `InvalidOperationException: HttpContext is null` | `IHttpContextAccessor` used in a non-request context | Ensure the service is `Scoped`, not `Singleton` |
| Redirect loop on login page | `[Authorize]` attribute present but auth middleware not configured | Confirm `app.UseAuthentication()` is before `app.UseAuthorization()` in `Program.cs` |
| Cache not invalidating after mutation | `[OutputCache]` in use with no eviction | Call `IOutputCacheStore.EvictByTagAsync("users", ct)` after write operations |
| Enhanced Navigation not working | `blazor.web.js` not loaded | Ensure `<script src="_framework/blazor.web.js"></script>` is at end of `<body>` in `App.razor` |
| Styles not loading (unstyled MudBlazor) | Missing CSS import | Confirm `<link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />` is in `App.razor` |
| `Missing <MudPopoverProvider />` at runtime | MudBlazor overlay stack incomplete | Add `<MudPopoverProvider />` in `Components/Layout/ThemeProvider.razor` before dialogs/snackbars |
| `CS8618`: Non-nullable property uninitialized | Strict nullable enabled | Initialise in constructor or use `= default!` for properties injected by the framework |
| Component not re-rendering on service change | Missing `StateHasChanged()` call | Subscribe to `ServiceName.OnChange += StateHasChanged` and unsubscribe in `Dispose()` |
| `RemoteNavigationManager has not been initialized` | Blazor-scoped service (e.g. `ISnackbar`) injected into an HTTP `DelegatingHandler` that is constructed before a circuit exists | Remove Blazor-scoped dependencies from `DelegatingHandler`; call `INotificationService` from the page's `catch` block instead |
| Login reports wrong-password for valid credentials | API returns `accessToken` but `User.Token` property has no `[JsonPropertyName]` | Add `[property: JsonPropertyName("accessToken")]` to the `Token` parameter in the `User` record |
| The POST request does not specify which form is being submitted | Static SSR form post with unnamed `<EditForm>` | Add a unique `FormName` to each `<EditForm>` (or `@formname` on raw `<form>`) |
| Form validation not triggering | Missing `<DataAnnotationsValidator />` | Add `<DataAnnotationsValidator />` inside `<EditForm>` |
| Page shows error state after token expiry instead of redirecting to login | `AuthTokenHandler` did not handle `401` responses from the API | Handler now calls `SignOutAsync` and `Redirect("/login?error=session_expired")` on `401`; login page maps the error to a friendly message |

<!-- RESOLVED — antiforgery row removed from table (Markdown tables don't support inline comments).
| `A valid antiforgery token was not provided` on `/account/login` | `IAntiforgery.GetAndStoreTokens()` called during render — response headers already written, so the antiforgery cookie is never sent | Use the built-in `<AntiforgeryToken />` component instead of manually injecting `IAntiforgery`; it integrates with the SSR pipeline and emits the cookie at the correct point |
-->

### Debugging the DI Container

.NET's built-in container throws descriptive exceptions at startup for misconfigured services:

```
System.InvalidOperationException:
  Some services are not able to be constructed (Error while validating the service descriptor
  'ServiceType: IAuthService Lifetime: Scoped ImplementationType: AuthService'):
  Unable to resolve service for type 'IErpHttpClient' while attempting to activate 'AuthService'.
```

**Fix**: Enable service validation at startup to catch all DI errors before the first request:

```csharp
// In Program.cs
if (app.Environment.IsDevelopment())
{
    // Equivalent of services.debugListServices() — validates all registrations eagerly
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    });
}
```

### Debugging Output Cache

To inspect the output cache in development, disable it globally and verify data freshness:

```csharp
// In appsettings.Development.json — disable output cache for debugging
{
  "OutputCache": {
    "DefaultExpirationTimeSpan": "00:00:01"
  }
}
```

Or evict a specific tag from a page after a mutation:

```csharp
@inject IOutputCacheStore CacheStore

// After a delete or create operation:
await CacheStore.EvictByTagAsync("users", CancellationToken.None);
```

### Structured Logging in Development

```bash
# Filter logs by level (equivalent of console filter by "level":"error")
dotnet run | grep '"LogLevel":"Error"'

# Or use the built-in development console filter in appsettings.Development.json:
# Set "Microsoft.AspNetCore": "Debug" to trace routing decisions
```

> [!TIP]
> **Production Logger Swap**
>
> Because all logging flows through `ILogger<T>`, swapping the sink in production requires only a NuGet package and a `builder.Logging.AddSentry(...)` call in `Program.cs` — no business logic changes required.

---

## 19. Typography: Libre Franklin <a name="typography"></a>

All text in the application uses **Libre Franklin** exclusively — a versatile, open-source humanist sans-serif from Google Fonts (SIL Open Font License). It is applied at two levels.

### 1. Google Fonts Import (`Components/App.razor`)

Load the full variable-font axis (weights 100–900, including italic) from the Google Fonts CDN:

```razor
<link href="https://fonts.googleapis.com/css2?family=Libre+Franklin:ital,wght@0,100..900;1,100..900&display=swap"
      rel="stylesheet" />
```

> [!TIP]
> **Variable-font axis `wght@0,100..900;1,100..900`**
>
> This single request loads the entire weight/italic range as a variable font, which is more efficient than requesting individual weights (e.g., `wght@300,400,600,700`). The browser only downloads the glyph subsets actually used on each page.

### 2. Global CSS Reset (`wwwroot/app.css`)

Apply the font to every element via the universal selector so all HTML — including MudBlazor components, plain Blazor markup, and third-party components — inherits Libre Franklin:

```css
/* ── Global Font ─────────────────────────────────────────────────────────── */
*, *::before, *::after {
    font-family: 'Libre Franklin', sans-serif;
}
```

> [!NOTE]
> **Why not the MudBlazor `Typography` block?**
>
> MudBlazor v9's Typography slot types (`Default`, `H1`, `H6`, `Button`, etc.) are short class names that collide with other C# identifiers when `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` is active, causing `CS0246` build failures. The CSS universal selector achieves the same result — overriding MudBlazor's built-in Roboto — without any build-time ambiguity. The `Typography` block in `ThemeProvider.razor` is therefore omitted; the CSS layer handles font enforcement entirely.

---

## 20. ASP.NET Core Web API Gateway (DummyJSON Wrapper) <a name="web-api-gateway"></a>

This section introduces a **companion Web API project** — `ErpPortal.Api` — that sits between the Blazor SSR front-end and the DummyJSON upstream. It authenticates against `POST /auth/login`, captures the JWT `accessToken` and `refreshToken`, and transparently injects the Bearer token into subsequent calls to protected routes like `/auth/products` and `/auth/todos`.

> [!IMPORTANT]
> **Why a Separate Web API Project?**
>
> In the main guide, the Blazor SSR app calls DummyJSON directly via `IErpHttpClient`. That approach works well for a monolith. This API gateway pattern is the correct choice when:
> - Multiple front-ends (Blazor, mobile, third-party) need a **single authenticated proxy**.
> - You want to **encapsulate upstream JWT lifecycle** (acquire → cache → refresh → retry) in one place.
> - You need to **add your own authorization, rate-limiting, or audit logging** before forwarding to DummyJSON.
> - The front-end should never see or manage the DummyJSON JWT — it authenticates to *your* API only.

### Architecture

```text
┌──────────────────┐         ┌──────────────────────┐         ┌──────────────────┐
│  Blazor SSR App  │ ──────► │  ErpPortal.Api       │ ──────► │  dummyjson.com   │
│  (or any client) │  HTTP   │  (Web API Gateway)   │  HTTP   │  (upstream)      │
│                  │         │                      │         │                  │
│  Uses cookie     │         │  • POST /api/auth    │         │  POST /auth/login│
│  auth to talk    │         │  • GET /api/products │         │  GET /auth/prods │
│  to this API     │         │  • GET /api/todos    │         │  GET /auth/todos │
│                  │         │  • Token mgmt svc    │         │                  │
└──────────────────┘         └──────────────────────┘         └──────────────────┘
```

### 20.1 Project Setup

```bash
# From the solution root
dotnet new webapi -n ErpPortal.Api --no-openapi false
cd ErpPortal.Api

# Add to existing solution (optional)
cd ..
dotnet sln add ErpPortal.Api/ErpPortal.Api.csproj
```

### 20.2 Project Structure

```text
ErpPortal.Api/
├── Controllers/
│   ├── AuthController.cs          # POST /api/auth/login → proxies to DummyJSON /auth/login
│   ├── ProductsController.cs      # GET /api/products    → proxies to DummyJSON /auth/products
│   └── TodosController.cs         # GET /api/todos       → proxies to DummyJSON /auth/todos
├── Core/
│   ├── Contracts/
│   │   ├── IDummyJsonClient.cs    # Typed HttpClient interface for upstream calls
│   │   └── ITokenService.cs       # JWT lifecycle: acquire, cache, refresh
│   └── Domain/
│       ├── AuthTokens.cs           # accessToken + refreshToken record
│       ├── Product.cs              # Product domain model
│       └── Todo.cs                 # Todo domain model (reuse or redefine)
├── Infrastructure/
│   ├── Http/
│   │   ├── DummyJsonAuthHandler.cs # DelegatingHandler — injects Bearer token automatically
│   │   └── DummyJsonClient.cs      # Concrete typed HttpClient
│   └── Services/
│       └── TokenService.cs         # In-memory JWT cache with auto-refresh
├── appsettings.json
└── Program.cs                      # DI composition root + middleware
```

### 20.3 Configuration (`appsettings.json`)

```json
{
  "Jwt": {
    "Issuer": "ErpPortal.Api",
    "Audience": "ErpPortal"
  },
  "DummyJson": {
    "BaseUrl": "https://dummyjson.com",
    "Username": "emilys",
    "Password": "emilyspass",
    "TokenExpiryMinutes": 30
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

The JWT signing secret (`Jwt:Secret`) is **never stored in `appsettings.json`**. Set it via user secrets locally and via environment variable in production:

```bash
# Local development — generates and stores a 256-bit random secret
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$bytes = New-Object byte[] 32
$rng.GetBytes($bytes)
$secret = [System.Convert]::ToBase64String($bytes)
dotnet user-secrets set "Jwt:Secret" $secret --project ErpPortal.Api/ErpPortal.Api.csproj
```

> [!CAUTION]
> **Credentials in `appsettings.json`**
>
> The `Username` and `Password` above are DummyJSON test credentials committed solely for demonstration. In production, store real upstream credentials in `dotnet user-secrets` locally and in environment variables (`DummyJson__Username`, `DummyJson__Password`) in CI/hosting. Never commit secrets to source control.

### 20.4 Configuration Options Class

```csharp
// Core/Config/DummyJsonSettings.cs
using System.ComponentModel.DataAnnotations;

namespace ErpPortal.Api.Core.Config;

public sealed class DummyJsonSettings
{
    public const string SectionName = "DummyJson";

    [Required, Url]
    public string BaseUrl { get; init; } = string.Empty;

    [Required]
    public string Username { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    [Range(1, 1440)]
    public int TokenExpiryMinutes { get; init; } = 30;
}
```

### 20.5 Domain Models

#### `Core/Domain/AuthTokens.cs`

```csharp
using System.Text.Json.Serialization;

namespace ErpPortal.Api.Core.Domain;

/// <summary>
/// Represents the token pair returned by DummyJSON's /auth/login endpoint.
/// </summary>
public sealed record AuthTokens(
    [property: JsonPropertyName("accessToken")]  string AccessToken,
    [property: JsonPropertyName("refreshToken")] string RefreshToken
);
```

#### `Core/Domain/Product.cs`

```csharp
using System.Text.Json.Serialization;

namespace ErpPortal.Api.Core.Domain;

public sealed record Product(
    [property: JsonPropertyName("id")]          int Id,
    [property: JsonPropertyName("title")]       string Title,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("price")]       decimal Price,
    [property: JsonPropertyName("brand")]       string? Brand,
    [property: JsonPropertyName("category")]    string Category,
    [property: JsonPropertyName("thumbnail")]   string Thumbnail
);

public sealed record ProductsResponse(
    [property: JsonPropertyName("products")] List<Product> Products,
    [property: JsonPropertyName("total")]    int Total,
    [property: JsonPropertyName("skip")]     int Skip,
    [property: JsonPropertyName("limit")]    int Limit
);
```

#### `Core/Domain/Todo.cs`

```csharp
using System.Text.Json.Serialization;

namespace ErpPortal.Api.Core.Domain;

public sealed record Todo(
    [property: JsonPropertyName("id")]        int Id,
    [property: JsonPropertyName("todo")]      string TodoText,
    [property: JsonPropertyName("completed")] bool Completed,
    [property: JsonPropertyName("userId")]    int UserId
);

public sealed record TodosResponse(
    [property: JsonPropertyName("todos")] List<Todo> Todos,
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("skip")]  int Skip,
    [property: JsonPropertyName("limit")] int Limit
);
```

### 20.6 Contracts (Interfaces)

#### `Core/Contracts/ITokenService.cs`

```csharp
namespace ErpPortal.Api.Core.Contracts;

/// <summary>
/// Manages the DummyJSON JWT lifecycle:
/// acquire on first call, cache in memory, refresh before expiry.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Returns a valid access token. Authenticates or refreshes automatically.
    /// </summary>
    Task<string> GetAccessTokenAsync(CancellationToken ct = default);

    /// <summary>
    /// Forces a fresh login, discarding cached tokens.
    /// </summary>
    Task InvalidateAsync();
}
```

#### `Core/Contracts/IDummyJsonClient.cs`

```csharp
using ErpPortal.Api.Core.Domain;

namespace ErpPortal.Api.Core.Contracts;

/// <summary>
/// Typed HttpClient wrapper for DummyJSON's protected /auth/* endpoints.
/// The Bearer token is injected transparently by <see cref="DummyJsonAuthHandler"/>.
/// </summary>
public interface IDummyJsonClient
{
    Task<ProductsResponse> GetProductsAsync(int skip = 0, int limit = 30, CancellationToken ct = default);
    Task<Product> GetProductByIdAsync(int id, CancellationToken ct = default);
    Task<TodosResponse> GetTodosAsync(int skip = 0, int limit = 30, CancellationToken ct = default);
    Task<Todo> GetTodoByIdAsync(int id, CancellationToken ct = default);
}
```

### 20.7 Token Management Service

This is the core piece — a singleton service that handles the full JWT lifecycle against DummyJSON.

```csharp
// Infrastructure/Services/TokenService.cs
using System.Net.Http.Json;
using System.Text.Json;
using ErpPortal.Api.Core.Config;
using ErpPortal.Api.Core.Contracts;
using ErpPortal.Api.Core.Domain;
using Microsoft.Extensions.Options;

namespace ErpPortal.Api.Infrastructure.Services;

/// <summary>
/// Singleton service that acquires a DummyJSON JWT via /auth/login,
/// caches both tokens in memory, and transparently refreshes via /auth/refresh
/// before the access token expires.
///
/// This is the .NET equivalent of an Axios interceptor that silently
/// refreshes a token on 401 — but proactive rather than reactive.
/// </summary>
public sealed class TokenService : ITokenService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DummyJsonSettings _settings;
    private readonly ILogger<TokenService> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private string? _accessToken;
    private string? _refreshToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public TokenService(
        IHttpClientFactory httpClientFactory,
        IOptions<DummyJsonSettings> settings,
        ILogger<TokenService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        // Fast path: token is still valid (with 60-second safety margin)
        if (_accessToken is not null && DateTimeOffset.UtcNow.AddSeconds(60) < _expiresAt)
        {
            return _accessToken;
        }

        // Serialize access: only one thread can acquire/refresh at a time
        await _semaphore.WaitAsync(ct);
        try
        {
            // Double-check after acquiring the lock
            if (_accessToken is not null && DateTimeOffset.UtcNow.AddSeconds(60) < _expiresAt)
            {
                return _accessToken;
            }

            // Try refresh first if we have a refresh token
            if (_refreshToken is not null)
            {
                try
                {
                    await RefreshAsync(ct);
                    return _accessToken!;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Token refresh failed — falling back to full login");
                }
            }

            // Full login
            await LoginAsync(ct);
            return _accessToken!;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task InvalidateAsync()
    {
        _accessToken = null;
        _refreshToken = null;
        _expiresAt = DateTimeOffset.MinValue;
        _logger.LogInformation("Token cache invalidated");
        return Task.CompletedTask;
    }

    private async Task LoginAsync(CancellationToken ct)
    {
        _logger.LogInformation(
            "Authenticating to DummyJSON as {Username}", _settings.Username);

        using HttpClient http = _httpClientFactory.CreateClient("DummyJsonRaw");

        HttpResponseMessage response = await http.PostAsJsonAsync(
            "/auth/login",
            new
            {
                username = _settings.Username,
                password = _settings.Password,
                expiresInMins = _settings.TokenExpiryMinutes,
            },
            _jsonOptions,
            ct);

        response.EnsureSuccessStatusCode();

        AuthTokens tokens = await response.Content
            .ReadFromJsonAsync<AuthTokens>(_jsonOptions, ct)
            ?? throw new InvalidOperationException("Null response from /auth/login");

        _accessToken  = tokens.AccessToken;
        _refreshToken = tokens.RefreshToken;
        _expiresAt    = DateTimeOffset.UtcNow.AddMinutes(_settings.TokenExpiryMinutes);

        _logger.LogInformation(
            "DummyJSON login successful. Token expires at {ExpiresAt:u}", _expiresAt);
    }

    private async Task RefreshAsync(CancellationToken ct)
    {
        _logger.LogInformation("Refreshing DummyJSON access token");

        using HttpClient http = _httpClientFactory.CreateClient("DummyJsonRaw");

        HttpResponseMessage response = await http.PostAsJsonAsync(
            "/auth/refresh",
            new
            {
                refreshToken = _refreshToken,
                expiresInMins = _settings.TokenExpiryMinutes,
            },
            _jsonOptions,
            ct);

        response.EnsureSuccessStatusCode();

        AuthTokens tokens = await response.Content
            .ReadFromJsonAsync<AuthTokens>(_jsonOptions, ct)
            ?? throw new InvalidOperationException("Null response from /auth/refresh");

        _accessToken  = tokens.AccessToken;
        _refreshToken = tokens.RefreshToken;
        _expiresAt    = DateTimeOffset.UtcNow.AddMinutes(_settings.TokenExpiryMinutes);

        _logger.LogInformation(
            "Token refreshed. New expiry: {ExpiresAt:u}", _expiresAt);
    }
}
```

> [!NOTE]
> **Thread-Safety with `SemaphoreSlim`**
>
> The `SemaphoreSlim(1, 1)` ensures that only one thread at a time can acquire or refresh the token. Without this, a burst of concurrent requests hitting an expired token would all race to call `/auth/login` simultaneously — wasting upstream quota and risking rate-limit errors. The double-check pattern after acquiring the lock prevents redundant logins.

### 20.8 DelegatingHandler — Transparent JWT Injection

This handler automatically injects the Bearer token into every outgoing request made by the `IDummyJsonClient`. It is the exact equivalent of the `AuthTokenHandler` in the Blazor SSR project, but instead of reading the token from cookie claims, it reads from `ITokenService`.

```csharp
// Infrastructure/Http/DummyJsonAuthHandler.cs
using System.Net;
using System.Net.Http.Headers;
using ErpPortal.Api.Core.Contracts;

namespace ErpPortal.Api.Infrastructure.Http;

/// <summary>
/// DelegatingHandler that injects "Authorization: Bearer {token}" into
/// every request made by the typed DummyJsonClient.
///
/// If the upstream returns 401, the handler invalidates the cached token
/// and retries the request once with a fresh token — the "retry on 401" pattern.
/// </summary>
public sealed class DummyJsonAuthHandler : DelegatingHandler
{
    private readonly ITokenService _tokenService;
    private readonly ILogger<DummyJsonAuthHandler> _logger;

    public DummyJsonAuthHandler(
        ITokenService tokenService,
        ILogger<DummyJsonAuthHandler> logger)
    {
        _tokenService = tokenService;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Attach the current access token
        string token = await _tokenService.GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

        // If 401 → token may have expired between our check and the upstream call.
        // Invalidate and retry exactly once.
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning(
                "Received 401 from {Url} — invalidating token and retrying",
                request.RequestUri);

            await _tokenService.InvalidateAsync();

            // Clone the request (original is disposed after first send)
            using HttpRequestMessage retryRequest = await CloneRequestAsync(request);
            string freshToken = await _tokenService.GetAccessTokenAsync(cancellationToken);
            retryRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", freshToken);

            response = await base.SendAsync(retryRequest, cancellationToken);
        }

        return response;
    }

    /// <summary>
    /// Clones an HttpRequestMessage because a sent message cannot be reused.
    /// </summary>
    private static async Task<HttpRequestMessage> CloneRequestAsync(
        HttpRequestMessage original)
    {
        HttpRequestMessage clone = new(original.Method, original.RequestUri);

        if (original.Content is not null)
        {
            byte[] content = await original.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(content);
            foreach (KeyValuePair<string, IEnumerable<string>> header
                in original.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(
                    header.Key, header.Value);
            }
        }

        foreach (KeyValuePair<string, IEnumerable<string>> header
            in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}
```

> [!TIP]
> **Retry-on-401 Pattern**
>
> The handler retries exactly once on a `401 Unauthorized` response. This covers the edge case where the token expires between the `GetAccessTokenAsync` call and the upstream receiving the request (clock skew, network latency). The retry uses a fresh token after invalidating the cache. If the retry also returns 401, the error propagates to the caller — preventing infinite loops.

### 20.9 Typed HttpClient (`DummyJsonClient`)

```csharp
// Infrastructure/Http/DummyJsonClient.cs
using System.Net.Http.Json;
using System.Text.Json;
using ErpPortal.Api.Core.Contracts;
using ErpPortal.Api.Core.Domain;

namespace ErpPortal.Api.Infrastructure.Http;

/// <summary>
/// Typed HttpClient for DummyJSON's authenticated endpoints.
/// The Bearer token is injected by <see cref="DummyJsonAuthHandler"/> —
/// this class never touches tokens directly.
/// </summary>
public sealed class DummyJsonClient : IDummyJsonClient
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public DummyJsonClient(HttpClient http) => _http = http;

    public async Task<ProductsResponse> GetProductsAsync(
        int skip = 0, int limit = 30, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<ProductsResponse>(
            $"/auth/products?limit={limit}&skip={skip}", _jsonOptions, ct)
            ?? throw new InvalidOperationException("Null response from /auth/products");
    }

    public async Task<Product> GetProductByIdAsync(
        int id, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<Product>(
            $"/auth/products/{id}", _jsonOptions, ct)
            ?? throw new InvalidOperationException($"Null response from /auth/products/{id}");
    }

    public async Task<TodosResponse> GetTodosAsync(
        int skip = 0, int limit = 30, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<TodosResponse>(
            $"/auth/todos?limit={limit}&skip={skip}", _jsonOptions, ct)
            ?? throw new InvalidOperationException("Null response from /auth/todos");
    }

    public async Task<Todo> GetTodoByIdAsync(
        int id, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<Todo>(
            $"/auth/todos/{id}", _jsonOptions, ct)
            ?? throw new InvalidOperationException($"Null response from /auth/todos/{id}");
    }
}
```

### 20.10 API Controllers

#### `Controllers/AuthController.cs`

The `AuthController` proxies login credentials to DummyJSON to validate the user, then **issues its own signed JWT** in the response. Downstream clients never receive or store the DummyJSON token — they only work with the portal-issued JWT.

```csharp
using ErpPortal.Api.Core.Config;
using ErpPortal.Api.Core.Contracts;
using ErpPortal.Api.Core.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace ErpPortal.Api.Controllers;

/// <summary>
/// Exposes a login endpoint that triggers the DummyJSON JWT acquisition.
/// Useful for health checks and explicit token refresh requests.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JwtSettings _jwtSettings;

    public AuthController(
        ITokenService tokenService,
        IHttpClientFactory httpClientFactory,
        ILogger<AuthController> logger,
        JwtSettings jwtSettings)
    {
        _tokenService = tokenService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _jwtSettings = jwtSettings;
    }

    /// <summary>
    /// POST /api/auth/login — acquires a fresh DummyJSON token.
    /// Returns 200 with the token (for debugging/testing) or 500 on failure.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            using HttpClient http = _httpClientFactory.CreateClient("DummyJsonRaw");

            HttpResponseMessage response = await http.PostAsJsonAsync(
                "/auth/login",
                new { username = request.Username, password = request.Password,
                      expiresInMins = request.ExpiresInMins },
                JsonSerializerOptions.Default,
                ct);

            response.EnsureSuccessStatusCode();

            User user = await response.Content
                .ReadFromJsonAsync<User>(JsonSerializerOptions.Default, ct)
                ?? throw new InvalidOperationException("Null response from /auth/login");

            // Replace the DummyJSON token with a portal-signed JWT
            string portalToken = IssuePortalToken(user, request.ExpiresInMins);

            _logger.LogInformation("User login proxied via gateway for {Username}", request.Username);
            return Ok(user with { Token = portalToken });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire DummyJSON token");
            return StatusCode(500, new { error = "Authentication failed", detail = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/auth/invalidate — forces token cache invalidation.
    /// </summary>
    [HttpPost("invalidate")]
    public async Task<IActionResult> Invalidate()
    {
        await _tokenService.InvalidateAsync();
        return Ok(new { message = "Token cache cleared" });
    }

    private string IssuePortalToken(User user, int expiresInMins)
    {
        byte[] key = Encoding.UTF8.GetBytes(_jwtSettings.Secret);
        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Username),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        ];

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expiresInMins),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256),
        };

        var handler = new JwtSecurityTokenHandler();
        SecurityToken token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }

    public sealed class LoginRequest
    {
        [Required] public string Username { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
        [Range(1, 43200)] public int ExpiresInMins { get; set; } = 60;
    }
}
```

The corresponding `JwtSettings` class carries the issuer, audience, and secret via DI:

```csharp
// Core/Config/JwtSettings.cs
namespace ErpPortal.Api.Core.Config;

public sealed record JwtSettings(string Secret, string Issuer, string Audience);
```

#### `Controllers/ProductsController.cs`

```csharp
using ErpPortal.Api.Core.Contracts;
using ErpPortal.Api.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpPortal.Api.Controllers;

/// <summary>
/// Proxies requests to DummyJSON's /auth/products (protected endpoint).
/// The DummyJSON JWT is managed transparently by TokenService + DummyJsonAuthHandler.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class ProductsController : ControllerBase
{
    private readonly IDummyJsonClient _client;

    public ProductsController(IDummyJsonClient client) => _client = client;

    [HttpGet]
    public async Task<ActionResult<ProductsResponse>> GetAll(
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 30,
        CancellationToken ct = default)
    {
        ProductsResponse result = await _client.GetProductsAsync(skip, limit, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Product>> GetById(
        int id, CancellationToken ct = default)
    {
        Product product = await _client.GetProductByIdAsync(id, ct);
        return Ok(product);
    }
}
```

#### `Controllers/TodosController.cs`

```csharp
using ErpPortal.Api.Core.Contracts;
using ErpPortal.Api.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpPortal.Api.Controllers;

/// <summary>
/// Proxies requests to DummyJSON's /auth/todos (protected endpoint).
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class TodosController : ControllerBase
{
    private readonly IDummyJsonClient _client;

    public TodosController(IDummyJsonClient client) => _client = client;

    [HttpGet]
    public async Task<ActionResult<TodosResponse>> GetAll(
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 30,
        CancellationToken ct = default)
    {
        TodosResponse result = await _client.GetTodosAsync(skip, limit, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Todo>> GetById(
        int id, CancellationToken ct = default)
    {
        Todo todo = await _client.GetTodoByIdAsync(id, ct);
        return Ok(todo);
    }
}
```

### 20.11 `Program.cs` — Composition Root

```csharp
// ErpPortal.Api/Program.cs
using ErpPortal.Api.Core.Config;
using ErpPortal.Api.Core.Contracts;
using ErpPortal.Api.Infrastructure.Http;
using ErpPortal.Api.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ─── JWT Authentication ───────────────────────────────────────────────────────
// Jwt:Secret must come from user secrets (dev) or environment variable (prod).
string jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException(
        "Jwt:Secret is not configured. Set it via User Secrets or environment variable.");
string jwtIssuer   = builder.Configuration["Jwt:Issuer"]   ?? "ErpPortal.Api";
string jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ErpPortal";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer   = true,
            ValidIssuer      = jwtIssuer,
            ValidateAudience = true,
            ValidAudience    = jwtAudience,
            ValidateLifetime = true,
            ClockSkew        = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

// ─── JWT Settings (injected into AuthController via DI) ──────────────────────
builder.Services.AddSingleton(new JwtSettings(jwtSecret, jwtIssuer, jwtAudience));

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
app.UseAuthentication();   // must come before UseAuthorization
app.UseAuthorization();
app.MapControllers();
app.Run();
```

> [!IMPORTANT]
> **Two Named HttpClients — Why?**
>
> The `"DummyJsonRaw"` named client has **no** `DummyJsonAuthHandler` attached. It is used exclusively by `TokenService` to call `/auth/login` and `/auth/refresh` — endpoints that don't require (and shouldn't have) a Bearer token. The typed `IDummyJsonClient` registration uses the auth handler for all `/auth/products` and `/auth/todos` calls. This prevents a circular dependency where the handler needs a token but the token service needs an HttpClient.

### 20.12 Project File (`ErpPortal.Api.csproj`)

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <RootNamespace>ErpPortal.Api</RootNamespace>
    <UserSecretsId>erp-portal-api-jwt</UserSecretsId>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.*" />
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.*" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="10.*" />
  </ItemGroup>

</Project>
```

> [!NOTE]
> **`UserSecretsId`**
>
> The `UserSecretsId` element enables `dotnet user-secrets` for this project so that `Jwt:Secret` can be stored outside the source tree during local development. In production, supply `Jwt__Secret` as an environment variable or via Azure Key Vault.

### 20.13 Running & Testing the API

```bash
# Start the API
cd ErpPortal.Api
dotnet run
# → Listening on https://localhost:5002

# Open Swagger UI
# https://localhost:5002/swagger
```

#### Quick Smoke Test with `curl`

```bash
# 1. Login — WebAPI validates against DummyJSON, returns a portal-signed JWT
curl -s -X POST https://localhost:5002/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"emilys","password":"emilyspass","expiresInMins":60}'
# → { "id": 1, "username": "emilys", ..., "accessToken": "eyJhbGciOiJIUzI1NiIs..." }

# Save the token for subsequent requests
TOKEN=$(curl -s -X POST https://localhost:5002/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"emilys","password":"emilyspass"}' | jq -r .accessToken)

# 2. Fetch protected products — Bearer token is required (returns 401 without it)
curl https://localhost:5002/api/products?limit=3 \
  -H "Authorization: Bearer $TOKEN"
# → { "products": [...], "total": 194, "skip": 0, "limit": 3 }

# 3. Fetch protected todos
curl https://localhost:5002/api/todos?limit=5 \
  -H "Authorization: Bearer $TOKEN"
# → { "todos": [...], "total": 254, "skip": 0, "limit": 5 }

# 4. Confirm unauthenticated request is rejected
curl -o /dev/null -w "%{http_code}" https://localhost:5002/api/products
# → 401

# 5. Force token invalidation (next DummyJSON service call will re-authenticate)
curl -X POST https://localhost:5002/api/auth/invalidate \
  -H "Authorization: Bearer $TOKEN"
# → { "message": "Token cache cleared" }
```

### 20.14 Connecting the Blazor SSR App to the API Gateway

To have the main Blazor SSR app talk to this API gateway instead of DummyJSON directly, update its `appsettings.json`:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:5002/api"
  }
}
```

And update the repository endpoints from `/users` to `/products`, `/todos`, etc. The Blazor app no longer manages any DummyJSON JWT — the API gateway handles it entirely.

### 20.15 Debugging the API Gateway

| Symptom | Cause | Fix |
|---|---|---|
| `401` from DummyJSON on first request | Credentials in `appsettings.json` are wrong | Verify `DummyJson:Username` and `DummyJson:Password` match a valid user from `dummyjson.com/users` |
| Infinite retry loop on 401 | `DummyJsonAuthHandler` retries forever | Handler retries exactly once; check that `TokenService.InvalidateAsync()` clears the cache |
| `InvalidOperationException: Null response` | DummyJSON returned unexpected JSON shape | Verify `[JsonPropertyName]` attributes match the actual API response fields |
| Circular dependency at startup | `DummyJsonAuthHandler` → `ITokenService` → `IHttpClientFactory` → `DummyJsonAuthHandler` | `TokenService` uses the `"DummyJsonRaw"` named client (no handler); the typed client uses the handler. No cycle. |
| Token expires during long-running batch | Proactive refresh margin too small | Increase the `60-second` safety margin in `TokenService.GetAccessTokenAsync` or decrease `TokenExpiryMinutes` |
| `SemaphoreSlim` deadlock | `await` inside a `lock` statement | The code correctly uses `SemaphoreSlim.WaitAsync()` (async-safe). Never use `lock` with `await`. |
| `InvalidOperationException: Jwt:Secret is not configured` at startup | User secret not set | Run `dotnet user-secrets set "Jwt:Secret" <base64-key>` or set `Jwt__Secret` environment variable |
| `401` returned by the WebAPI for all data endpoints | `[Authorize]` present but `UseAuthentication()` missing or ordered after `UseAuthorization()` | Ensure `app.UseAuthentication()` appears **before** `app.UseAuthorization()` in `Program.cs` |
| `IDX10501: Signature validation failed` | Client is sending the DummyJSON token instead of the portal JWT | After login, the client must store and forward the `accessToken` field from the login response, which is now the portal-signed JWT — not the DummyJSON one |
| `IDX10223: Lifetime validation failed` | Portal JWT has expired | `ClockSkew = TimeSpan.Zero` is intentional. Client must re-authenticate. Increase `ExpiresInMins` in the login request if sessions are too short. |

> [!TIP]
> **Extending the Gateway**
>
> To add more DummyJSON protected routes (e.g., `/auth/carts`, `/auth/users`), simply:
> 1. Add the domain record in `Core/Domain/`.
> 2. Add the method signature to `IDummyJsonClient`.
> 3. Implement it in `DummyJsonClient` pointing to `/auth/{resource}`.
> 4. Create a new controller in `Controllers/`.
>
> The token lifecycle, handler, and retry logic are fully reusable — zero changes needed in the infrastructure layer.

---
