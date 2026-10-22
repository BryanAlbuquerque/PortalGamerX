using System.Globalization;

namespace EcommerceBase.Application.Common;

public class Result
{
    protected Result(bool succeeded, string? error)
    {
        Succeeded = succeeded;
        Error = error;
    }

    public bool Succeeded { get; }
    public string? Error { get; }

    public static Result Ok() => new(true, null);
    public static Result Fail(string error) => new(false, error);
}

public class Result<T> : Result
{
    private Result(bool succeeded, T? value, string? error) : base(succeeded, error)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Ok(T value) => new(true, value, null);
    public static new Result<T> Fail(string error) => new(false, default, error);
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 12;
    public int TotalCount { get; init; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

public sealed record SelectOption(string Value, string Text);

/// <summary>Arquivo enviado, desacoplado do ASP.NET (a camada Web converte IFormFile).</summary>
public sealed record UploadedFile(Stream Content, string FileName, long Length);

/// <summary>Identificação do dono do carrinho: usuário autenticado e/ou token de visitante.</summary>
public readonly record struct CartOwner(Guid? UserId, string? AnonymousToken);

/// <summary>Formatação pt-BR independente de ICU/cultura do servidor.</summary>
public static class BrFormat
{
    private static readonly NumberFormatInfo Nfi = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = ".",
        CurrencyDecimalSeparator = ",",
        CurrencyGroupSeparator = ".",
        CurrencySymbol = "R$",
        CurrencyPositivePattern = 2,  // $ n
        CurrencyNegativePattern = 9   // -$ n
    };

    public static string Money(decimal value) => value.ToString("C", Nfi);

    public static string Number(decimal value, int decimals = 2) => value.ToString("N" + decimals, Nfi);

    /// <summary>Valor para campos de formulário (ex.: 1234,50), sem separador de milhar.</summary>
    public static string Input(decimal value) => value.ToString("0.00", Nfi);

    public static string Input(decimal? value) => value.HasValue ? Input(value.Value) : string.Empty;
}
