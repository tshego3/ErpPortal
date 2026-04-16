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
