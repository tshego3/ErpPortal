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
