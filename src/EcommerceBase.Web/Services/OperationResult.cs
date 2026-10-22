namespace EcommerceBase.Web.Services;

public record OperationResult(bool Success, string? Error = null)
{
    public static OperationResult Ok() => new(true);
    public static OperationResult Fail(string error) => new(false, error);
}

public record Actor(Guid? UserId, string Name)
{
    public static Actor System { get; } = new(null, "sistema");
}
