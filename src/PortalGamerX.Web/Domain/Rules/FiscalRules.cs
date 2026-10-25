using PortalGamerX.Domain.Entities;

namespace PortalGamerX.Domain.Rules;

/// <summary>
/// Regras fiscais puras para a venda de jogos (mídia física) a consumidor final pela internet.
/// São VALORES PADRÃO e verificações de consistência — a classificação definitiva do item e a
/// tributação (ICMS, ST, PIS/COFINS) dependem do regime da empresa e devem ser validadas pelo contador.
/// </summary>
public static class FiscalRules
{
    /// <summary>Jogos em disco óptico (CD/DVD/Blu-ray): 8523.49.90.</summary>
    public const string NcmOpticalDisc = "85234990";

    /// <summary>Cartuchos / game cards para consoles e portáteis: 9504.50.00 (acessório de console, Cap. 95).</summary>
    public const string NcmGameCartridge = "95045000";

    /// <summary>Unidade comercial padrão.</summary>
    public const string Unit = "UN";

    /// <summary>CFOP de venda de mercadoria adquirida de terceiros, dentro do estado.</summary>
    public const string CfopInState = "5102";

    /// <summary>CFOP de venda de mercadoria adquirida de terceiros a NÃO contribuinte, para outro estado.</summary>
    public const string CfopOutOfStateNonContributor = "6108";

    public static string DefaultNcm(GameMediaType mediaType) =>
        mediaType == GameMediaType.Cartridge ? NcmGameCartridge : NcmOpticalDisc;

    public static bool IsValidNcm(string? ncm) =>
        ncm is { Length: 8 } && ncm.All(char.IsDigit);

    public static bool IsValidCest(string? cest) =>
        string.IsNullOrEmpty(cest) || (cest.Length == 7 && cest.All(char.IsDigit));

    /// <summary>Origem da mercadoria: tabela da NF-e (0 a 8).</summary>
    public static bool IsValidOrigin(int origin) => origin is >= 0 and <= 8;

    public static string OriginLabel(int origin) => origin switch
    {
        0 => "0 - Nacional",
        1 => "1 - Estrangeira (importação direta)",
        2 => "2 - Estrangeira (adquirida no mercado interno)",
        3 => "3 - Nacional (importação entre 40% e 70%)",
        4 => "4 - Nacional (processos produtivos básicos)",
        5 => "5 - Nacional (importação até 40%)",
        6 => "6 - Estrangeira (importação direta, sem similar nacional)",
        7 => "7 - Estrangeira (mercado interno, sem similar nacional)",
        8 => "8 - Nacional (importação acima de 70%)",
        _ => origin.ToString()
    };

    public static bool IsInterstate(string? issuerState, string? destinationState) =>
        !string.IsNullOrWhiteSpace(issuerState) && !string.IsNullOrWhiteSpace(destinationState) &&
        !string.Equals(issuerState.Trim(), destinationState.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// CFOP padrão da venda online a consumidor final não contribuinte: 5102 (mesmo estado) ou 6108 (outro estado).
    /// Retorna null se a UF do emitente não estiver cadastrada.
    /// </summary>
    public static string? Cfop(string? issuerState, string? destinationState)
    {
        if (string.IsNullOrWhiteSpace(issuerState) || string.IsNullOrWhiteSpace(destinationState)) return null;
        return IsInterstate(issuerState, destinationState) ? CfopOutOfStateNonContributor : CfopInState;
    }

    /// <summary>Descrição do item na nota: "Nome do jogo - Plataforma".</summary>
    public static string ItemDescription(string name, GamePlatform platform) =>
        platform == GamePlatform.Outro ? name : $"{name} - {platform.Label()}";

    /// <summary>
    /// Pendências que impedem a emissão. Lista vazia = dados mínimos presentes.
    /// </summary>
    public static List<string> CheckIssuer(StoreSettings s)
    {
        var problems = new List<string>();
        if (!Documents.IsValidCnpj(s.Cnpj)) problems.Add("CNPJ da empresa ausente ou inválido (Configurações › Empresa).");
        if (string.IsNullOrWhiteSpace(s.StateRegistration)) problems.Add("Inscrição estadual da empresa não informada (Configurações › Empresa).");
        if (string.IsNullOrWhiteSpace(s.State) || s.State.Trim().Length != 2) problems.Add("UF da empresa não informada (Configurações › Empresa).");
        if (!Documents.IsValidPostalCode(s.PostalCode)) problems.Add("CEP da empresa ausente ou inválido (Configurações › Empresa).");
        return problems;
    }

    public static List<string> CheckItem(Jogo g)
    {
        var problems = new List<string>();
        if (!IsValidNcm(g.EffectiveNcm)) problems.Add($"\"{g.Name}\": NCM inválido (use 8 dígitos).");
        if (!IsValidCest(g.Cest)) problems.Add($"\"{g.Name}\": CEST inválido (use 7 dígitos).");
        if (!IsValidOrigin(g.OriginCode)) problems.Add($"\"{g.Name}\": origem da mercadoria inválida.");
        return problems;
    }
}
