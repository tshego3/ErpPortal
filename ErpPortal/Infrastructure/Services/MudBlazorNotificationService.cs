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
