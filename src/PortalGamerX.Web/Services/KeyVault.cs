using Microsoft.AspNetCore.DataProtection;

namespace PortalGamerX.Services;

/// <summary>
/// Cofre das keys de ativação: cifra/decifra o código com ASP.NET Core Data Protection (as chaves do anel ficam no banco).
/// O texto cifrado só é decifrado ao entregar a key ao cliente ou quando o administrador pede para revelá-la.
/// </summary>
public interface IKeyVault
{
    string Protect(string code);
    /// <summary>Decifra a key. Retorna null se o texto cifrado não puder ser lido (ex.: anel de chaves perdido).</summary>
    string? Unprotect(string cipherText);
}

public class KeyVault : IKeyVault
{
    // NÃO renomear: faz parte da chave de criptografia; alterá-lo torna ilegíveis as keys já cadastradas.
    private const string Purpose = "PortalGamerX.GameKeys.v1";
    private readonly IDataProtector _protector;

    public KeyVault(IDataProtectionProvider provider) => _protector = provider.CreateProtector(Purpose);

    public string Protect(string code) => _protector.Protect(code);

    public string? Unprotect(string cipherText)
    {
        try { return _protector.Unprotect(cipherText); }
        catch (System.Security.Cryptography.CryptographicException) { return null; }
    }
}
