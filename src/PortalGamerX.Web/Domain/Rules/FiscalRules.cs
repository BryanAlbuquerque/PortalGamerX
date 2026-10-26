using System.Text.RegularExpressions;
using PortalGamerX.Domain.Entities;

namespace PortalGamerX.Domain.Rules;

/// <summary>Parâmetros fiscais do serviço (configurados no painel): são os mesmos para todos os jogos vendidos.</summary>
public record FiscalServiceParams(string ServiceCode, string? MunicipalTaxCode, string? Cnae, decimal IssRatePercent);

/// <summary>
/// Regras fiscais puras para a venda de keys de ativação de jogos pela internet.
/// Venda de key = licenciamento/cessão de direito de uso de programa de computação (item 1.05 da lista da LC 116/2003),
/// tributado por ISS e documentado por NFS-e (o STF afastou o ICMS nas ADIs 1945 e 5659). Os valores aqui são PADRÕES e
/// verificações de consistência: alíquota, código municipal, CNAE e regime dependem da empresa e do município e devem
/// ser validados pelo contador (a reforma tributária — IBS/CBS — também está em transição).
/// </summary>
public static class FiscalRules
{
    /// <summary>Item da lista de serviços (LC 116/2003): licenciamento ou cessão de direito de uso de programas de computação.</summary>
    public const string DefaultServiceCode = "01.05";

    public const string DocumentTypeNfse = "NFSe";

    public static bool IsValidServiceCode(string? code) => code != null && Regex.IsMatch(code, @"^\d{2}\.\d{2}$");

    /// <summary>CNAE: 7 dígitos (aceita pontuação).</summary>
    public static bool IsValidCnae(string? cnae) => string.IsNullOrEmpty(cnae) || Documents.OnlyDigits(cnae).Length == 7;

    /// <summary>Alíquota de ISS em %. A LC 116/2003 limita a alíquota máxima em 5%.</summary>
    public static bool IsValidIssRate(decimal rate) => rate is >= 0m and <= 5m;

    public static decimal IssAmount(decimal serviceValue, decimal ratePercent) =>
        Money.Round(Math.Max(serviceValue, 0m) * Math.Clamp(ratePercent, 0m, 100m) / 100m);

    /// <summary>Descrição do item na nota: licenciamento do jogo, com plataforma e loja de ativação.</summary>
    public static string ItemDescription(string name, GamePlatform platform, ActivationStore store)
    {
        var where = platform == GamePlatform.Outro ? store.Label() : $"{platform.Label()} / {store.Label()}";
        return $"Licença de uso de jogo (chave de ativação) - {name} - {where}";
    }

    /// <summary>Pendências do emitente (empresa) e dos parâmetros fiscais. Lista vazia = dados mínimos presentes.</summary>
    public static List<string> CheckIssuer(StoreSettings s, FiscalServiceParams p)
    {
        var problems = new List<string>();
        if (!Documents.IsValidCnpj(s.Cnpj)) problems.Add("CNPJ da empresa ausente ou inválido (Configurações › Empresa).");
        if (string.IsNullOrWhiteSpace(s.MunicipalRegistration)) problems.Add("Inscrição municipal da empresa não informada (Configurações › Empresa).");
        if (string.IsNullOrWhiteSpace(s.City)) problems.Add("Cidade da empresa não informada (Configurações › Empresa).");
        if (string.IsNullOrWhiteSpace(s.State) || s.State.Trim().Length != 2) problems.Add("UF da empresa não informada (Configurações › Empresa).");
        if (!Documents.IsValidPostalCode(s.PostalCode)) problems.Add("CEP da empresa ausente ou inválido (Configurações › Empresa).");
        if (!IsValidServiceCode(p.ServiceCode)) problems.Add("Item da lista de serviços inválido (use o formato 00.00, ex.: 01.05).");
        if (!IsValidCnae(p.Cnae)) problems.Add("CNAE inválido (7 dígitos).");
        if (!IsValidIssRate(p.IssRatePercent)) problems.Add("Alíquota de ISS inválida (0% a 5%).");
        return problems;
    }

    /// <summary>Pendências do tomador (cliente): o ISS deste serviço é devido no domicílio do tomador, então o endereço é necessário.</summary>
    public static List<string> CheckCustomer(Order o)
    {
        var problems = new List<string>();
        if (!string.IsNullOrWhiteSpace(o.CustomerDocument) && !Documents.IsValidCpfOrCnpj(o.CustomerDocument))
            problems.Add("CPF/CNPJ do cliente inválido.");
        if (string.IsNullOrWhiteSpace(o.BillingCity) || string.IsNullOrWhiteSpace(o.BillingState) || o.BillingState.Trim().Length != 2)
            problems.Add("Cidade/UF do cliente ausentes no pedido.");
        if (!Documents.IsValidPostalCode(o.BillingPostalCode)) problems.Add("CEP do cliente inválido no pedido.");
        return problems;
    }
}
