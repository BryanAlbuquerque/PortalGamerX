namespace EcommerceBase.Web.Integrations;

public class ProviderResult<T>
{
    public bool Success { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }

    public static ProviderResult<T> Ok(T value) => new() { Success = true, Value = value };
    public static ProviderResult<T> Fail(string error) => new() { Success = false, Error = error };
}
