---
name: gold-standard-state
description: Code-specific implementation patterns and templates for the ERP Portal — repository/typed-client patterns, the Interactive Server 4-branch Gold Standard state management pattern, the SSR-only 2-branch variant, and testability guidelines. Use when implementing or reviewing async data-loading components, repositories, or state management patterns.
---

# ERP Portal Code Patterns & Templates (.NET 10 Blazor SSR)

Copy-paste templates. Rules live in **global-rules**; delivery skills and checklists in **project-expert**. Motto: move fast, be clear, do not overcomplicate. **This file is capped at 200 lines** like every skill — at the cap, fold into an existing line or split into a reference file, never append. Comments in the templates below are teaching annotations; **in real code, comment only complex or non-obvious decisions** (global-rules 11.10).
> **Pattern selection:** SSR-only pages fetch once in `OnInitializedAsync`, render a single pass (no visible Loading spinner), use event-less markup, and follow the 2-branch pattern (HasData / Empty) with try/catch error handling surfaced via a message variable. Interactive Server pages (`@rendermode InteractiveServer`) follow the **4-branch pattern**: Loading → Error → Data → Empty.

## Domain Record (Core)
```csharp
// Core/Domain/MaintenanceJob.cs — immutable data holder, no logic; explicit types, never object/dynamic
public sealed record MaintenanceJob(int Id, string Title, string? Description, DateTime? ScheduledDate);
public enum JobStatus { None = 0, Scheduled = 1, InProgress = 2, Complete = 3 }   // explicit zero member
```
Every property uses an **explicit concrete type** — never `object`/`dynamic`; enums are never nullable (global-rules 11.4–11.5).

## Contract → Repository → Typed Client (no MediatR/CQRS)
```csharp
// Core/Contracts/IMaintenanceJobRepository.cs — all I/O behind an interface
public interface IMaintenanceJobRepository
{
    Task<(IReadOnlyList<MaintenanceJob> Data, int Total)> GetAllAsync(int skip = 0, int limit = 50, CancellationToken ct = default);
    Task<MaintenanceJob?> GetByIdAsync(int id, CancellationToken ct = default);
}
// Infrastructure/Repositories/MaintenanceJobRepository.cs — register with AddScoped<IMaintenanceJobRepository, …>()
public sealed class MaintenanceJobRepository(IErpHttpClient http, ILogger<MaintenanceJobRepository> logger)
    : IMaintenanceJobRepository
{
    public async Task<(IReadOnlyList<MaintenanceJob> Data, int Total)> GetAllAsync(
        int skip = 0, int limit = 50, CancellationToken ct = default)
    {
        logger.LogInformation("Fetching maintenance jobs skip {Skip} limit {Limit}.", skip, limit); // intent before
        MaintenanceJobsApiResponse response = await http.GetAsync<MaintenanceJobsApiResponse>(
            $"/maintenance-jobs?limit={limit}&skip={skip}", ct);
        logger.LogInformation("Fetched {Count} maintenance jobs.", response.Total);                 // outcome after
        return (response.Jobs, response.Total);
    }
}
internal sealed record MaintenanceJobsApiResponse(List<MaintenanceJob> Jobs, int Total);
```
Route aliases an external system depends on go as extra `[Http*]` attributes on the *same* action — never a duplicated controller (global-rules 6.7). Every route needs a known caller (global-rules 6.8).

## Gold Standard Component State (Interactive Server, code-behind)
Components hold only UI state; data access stays behind injected interfaces, so `LoadDataAsync` is testable by mocking them.
```csharp
public partial class YourComponentName          // Pages/YourComponent.razor.cs
{
    [Inject] public IMaintenanceJobRepository JobRepo { get; set; } = null!;
    [Inject] public ILogger<YourComponentName> Logger { get; set; } = null!;
    // Raw data + user input, never rendered directly.
    private List<MaintenanceJob> _data = new();
    private string _searchText = string.Empty;
    private string? _selectedCategory = null;
    // Three mandatory flags — public so the template binds without extra accessors
    public bool Loading { get; set; } = true;
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }
    // Computed — always recalculated, never cached
    public IEnumerable<MaintenanceJob> FilteredData => _data.Where(ApplySearchFilter).Where(ApplyCategoryFilter).ToList();
    public int ResultCount => FilteredData.Count();
    public bool HasSourceData => _data.Any();   // gate toolbar/table on source data, not filtered count

    protected override async Task OnInitializedAsync() => await LoadDataAsync();
    private async Task LoadDataAsync()
    {
        try
        {
            Loading = true; HasError = false; ErrorMessage = null;
            (IReadOnlyList<MaintenanceJob> data, int _) = await JobRepo.GetAllAsync(ct: CancellationToken.None);
            _data = [.. data];
            Logger.LogInformation("Loaded {Count} items.", _data.Count);
        }
        catch (Exception ex)           // network vs unexpected — both user-safe, never ex.Message
        {
            HasError = true;
            ErrorMessage = ex is HttpRequestException
                ? "Network error. Please check your connection and try again."
                : "Failed to load data. Please try again or contact support.";
            Logger.LogError(ex, "Error loading maintenance jobs");
            _data = new List<MaintenanceJob>();
        }
        finally { Loading = false; }   // the ONLY place Loading is cleared
    }
    private bool ApplySearchFilter(MaintenanceJob item) =>
        string.IsNullOrEmpty(_searchText) || item.Title.Contains(_searchText, StringComparison.OrdinalIgnoreCase);
    private bool ApplyCategoryFilter(MaintenanceJob item) => _selectedCategory == null || item.Description == _selectedCategory;
    private void ClearSearch() => _searchText = string.Empty;
    private void ResetFilters() { _searchText = string.Empty; _selectedCategory = null; HasError = false; ErrorMessage = null; }
    public async Task RefreshAsync() => await LoadDataAsync();
}
```

## Gold Standard Razor Template (4 branches, exact order)
```razor
@* Pages/YourComponent.razor — @page "/your-component" + @rendermode InteractiveServer + @attribute [Authorize] *@
@if (Loading)                        @* 1 LOADING *@
{
    <MudProgressCircular Indeterminate="true" />
}
else if (HasError)                   @* 2 ERROR — safe message + retry *@
{
    <MudAlert Severity="Severity.Error" Icon="@Icons.Material.Filled.ErrorOutline">
        <MudText Typo="Typo.body2">@ErrorMessage</MudText>
        <MudButton Variant="Variant.Text" Size="Size.Small" Color="Color.Error"
            StartIcon="@Icons.Material.Filled.Refresh" OnClick="RefreshAsync">Try Again</MudButton>
    </MudAlert>
}
else if (HasSourceData)              @* 3 DATA — toolbar + count + table *@
{
    <MudStack Row="true" Spacing="2" AlignItems="AlignItems.Center" Class="mb-4">
        <MudTextField @bind-Value="_searchText" Placeholder="Search..." Variant="Variant.Outlined" Margin="Margin.Dense"
            Adornment="Adornment.End" AdornmentIcon="@Icons.Material.Filled.Search" Class="flex-grow-1" />
        <MudSpacer />
        <MudButton Variant="Variant.Text" Size="Size.Small" OnClick="ResetFilters">Clear Filters</MudButton>
    </MudStack>
    <MudText Typo="Typo.caption" Class="mb-3">Showing @ResultCount result@(ResultCount != 1 ? "s" : "")</MudText>
    <MudTable Items="@FilteredData" Hover="true" Breakpoint="Breakpoint.Sm" Dense="true">
        <HeaderContent><MudTh>Title</MudTh><MudTh Style="text-align:right;">Actions</MudTh></HeaderContent>
        <RowTemplate>
            <MudTd DataLabel="Title">@context.Title</MudTd>
            <MudTd DataLabel="Actions" Style="text-align:right;"><MudButton Size="Size.Small" Variant="Variant.Text"
                Color="Color.Primary" href="@($"/your-component/{context.Id}")">View</MudButton></MudTd>
        </RowTemplate>
    </MudTable>
}
else                                 @* 4 EMPTY — guidance + create action *@
{
    <MudAlert Severity="Severity.Info" Icon="@Icons.Material.Filled.Info">
        <MudText Typo="Typo.body2">No items yet. Create your first item to get started.</MudText>
        <MudButton Variant="Variant.Filled" Color="Color.Primary" Size="Size.Small"
            StartIcon="@Icons.Material.Filled.Add" href="/your-component/create">Create Item</MudButton>
    </MudAlert>
}
```
When a filter matches nothing, render the zero-match message and a "Clear All Filters" button *inside* Branch 3, below the count — a filter must never collapse the page into Branch 4 and hide the controls needed to recover.

## SSR-only Variant (2 branches)
SSR pages render one server pass after `OnInitializedAsync`; there is no interactive `OnClick`. Fetch in `OnInitializedAsync` inside try/catch, store `_errorMessage`, then:
```razor
@if (!string.IsNullOrEmpty(_errorMessage))
{
    <MudAlert Severity="Severity.Error">@_errorMessage</MudAlert>   @* include guidance on how to proceed *@
}
else if (_data.Any())
{
    @* static table / list — links and forms only, no event callbacks *@
}
else
{
    <MudAlert Severity="Severity.Info">No items yet.</MudAlert>
}
```
Never add Loading/HasError flags to SSR-only pages — they are invisible on a single-pass render.

## Testing (xUnit + Moq + FluentAssertions)
```csharp
[Fact]   // error path: user-safe message, Loading cleared, exception logged
public async Task OnInitializedAsync_WithNetworkException_SetsSafeErrorMessage()
{
    Mock<IMaintenanceJobRepository> repo = new Mock<IMaintenanceJobRepository>();
    repo.Setup(r => r.GetAllAsync(0, 50, It.IsAny<CancellationToken>()))
        .ThrowsAsync(new HttpRequestException("Network failed"));
    Mock<ILogger<YourComponentName>> logger = new Mock<ILogger<YourComponentName>>();
    YourComponentName component = new YourComponentName { JobRepo = repo.Object, Logger = logger.Object };
    await component.OnInitializedAsync();
    component.Loading.Should().BeFalse();
    component.HasError.Should().BeTrue();
    component.ErrorMessage.Should().Contain("Network error");
    logger.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
        It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
}
```
Mock injected interfaces — no real HTTP in unit tests; `NullLogger<T>` unless asserting error-path logging; name tests `[Method]_[Scenario]_[ExpectedResult]`; assert one behaviour per test. Also cover: the success path (`Loading` false, `HasError` false, `ResultCount` correct, repository called once); `ResetFilters()` clearing all state; `FilteredData` matching filter criteria exactly; and repository guard clauses rejecting invalid input before any HTTP call (`await act.Should().ThrowAsync<ArgumentException>()` plus `http.Verify(…, Times.Never)`).
