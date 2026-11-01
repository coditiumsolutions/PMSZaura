namespace Salzee.Services;

public sealed class AmsDeleteResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";

    public static AmsDeleteResult Ok(string message = "Deleted.") =>
        new() { Success = true, Message = message };

    public static AmsDeleteResult Fail(string message) =>
        new() { Success = false, Message = message };
}
