namespace ErpPortal.Core.Contracts;

public interface INotificationService
{
    void ShowSuccess(string title, string message);
    void ShowError(string title, string message);
    void ShowInfo(string title, string message);
}
