using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using EcommerceBase.Domain.Rules;

namespace EcommerceBase.Application.Validation;

/// <summary>Campos vazios são válidos; combine com [Required] quando obrigatório.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CpfAttribute : ValidationAttribute
{
    public CpfAttribute() : base("CPF inválido.") { }
    public override bool IsValid(object? value) =>
        value is not string s || string.IsNullOrWhiteSpace(s) || BrazilianDocuments.IsValidCpf(s);
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CnpjAttribute : ValidationAttribute
{
    public CnpjAttribute() : base("CNPJ inválido (verifique os dígitos).") { }
    public override bool IsValid(object? value) =>
        value is not string s || string.IsNullOrWhiteSpace(s) || BrazilianDocuments.IsValidCnpj(s);
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CpfOrCnpjAttribute : ValidationAttribute
{
    public CpfOrCnpjAttribute() : base("CPF ou CNPJ inválido.") { }
    public override bool IsValid(object? value) =>
        value is not string s || string.IsNullOrWhiteSpace(s) || BrazilianDocuments.IsValidCpfOrCnpj(s);
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CepAttribute : ValidationAttribute
{
    public CepAttribute() : base("CEP inválido. Informe 8 dígitos.") { }
    public override bool IsValid(object? value) =>
        value is not string s || string.IsNullOrWhiteSpace(s) || BrazilianDocuments.IsValidCep(s);
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class StateCodeAttribute : ValidationAttribute
{
    public StateCodeAttribute() : base("UF inválida.") { }
    public override bool IsValid(object? value) =>
        value is not string s || string.IsNullOrWhiteSpace(s) || BrazilianDocuments.IsValidState(s);
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class HexColorAttribute : ValidationAttribute
{
    private static readonly Regex Pattern = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);

    public HexColorAttribute() : base("Cor inválida. Use o formato #RRGGBB.") { }
    public override bool IsValid(object? value) =>
        value is not string s || string.IsNullOrWhiteSpace(s) || Pattern.IsMatch(s);
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SafeUrlAttribute : ValidationAttribute
{
    public SafeUrlAttribute() : base("Informe um link relativo (/produtos) ou uma URL http(s).") { }

    public override bool IsValid(object? value)
    {
        if (value is not string s || string.IsNullOrWhiteSpace(s)) return true;
        s = s.Trim();
        if (s.StartsWith("/") && !s.StartsWith("//")) return true;
        return Uri.TryCreate(s, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
