namespace PortalGamerX.Infrastructure;

public interface IFileStorage
{
    /// <summary>Valida e salva a imagem. Retorna o caminho público (ex.: /media/jogos/abc.jpg).</summary>
    Task<(bool Success, string? Path, string? Error)> SaveImageAsync(IFormFile file, string folder, bool allowIco = false);
    Task DeleteAsync(string? publicPath);
    string RootPath { get; }
}

/// <summary>
/// Armazenamento em disco. Em hospedagens com disco efêmero (Render), aponte Storage:RootPath para um
/// disco persistente (ex.: /var/data/storage). Para escala maior, substitua por uma implementação S3/R2 desta interface.
/// </summary>
public class LocalFileStorage : IFileStorage
{
    public const string PublicPrefix = "/media";
    private const long MaxBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedFolders = { "jogos", "banners", "branding" };

    public string RootPath { get; }

    public LocalFileStorage(IConfiguration cfg, IWebHostEnvironment env)
    {
        var configured = cfg["Storage:RootPath"];
        RootPath = string.IsNullOrWhiteSpace(configured) ? Path.Combine(env.ContentRootPath, "storage") : configured;
        Directory.CreateDirectory(RootPath);
    }

    public static string? DetectExtension(ReadOnlySpan<byte> h, bool allowIco)
    {
        if (h.Length >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return ".jpg";
        if (h.Length >= 8 && h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47 && h[4] == 0x0D && h[5] == 0x0A && h[6] == 0x1A && h[7] == 0x0A) return ".png";
        if (h.Length >= 6 && h[0] == 'G' && h[1] == 'I' && h[2] == 'F' && h[3] == '8') return ".gif";
        if (h.Length >= 12 && h[0] == 'R' && h[1] == 'I' && h[2] == 'F' && h[3] == 'F' && h[8] == 'W' && h[9] == 'E' && h[10] == 'B' && h[11] == 'P') return ".webp";
        if (allowIco && h.Length >= 4 && h[0] == 0 && h[1] == 0 && h[2] == 1 && h[3] == 0) return ".ico";
        return null;
    }

    public async Task<(bool Success, string? Path, string? Error)> SaveImageAsync(IFormFile file, string folder, bool allowIco = false)
    {
        if (!AllowedFolders.Contains(folder)) return (false, null, "Pasta de destino inválida.");
        if (file == null || file.Length == 0) return (false, null, "Arquivo vazio.");
        if (file.Length > MaxBytes) return (false, null, "A imagem excede 5 MB.");

        var header = new byte[16];
        int read;
        await using (var s = file.OpenReadStream())
        {
            read = await s.ReadAsync(header.AsMemory(0, header.Length));
        }
        // A extensão vem do CONTEÚDO real (assinatura), nunca do nome enviado pelo cliente.
        var ext = DetectExtension(header.AsSpan(0, read), allowIco);
        if (ext == null) return (false, null, "Formato não suportado. Envie JPG, PNG, GIF ou WEBP" + (allowIco ? " ou ICO." : "."));

        var name = Guid.NewGuid().ToString("N") + ext;
        var dir = Path.Combine(RootPath, folder);
        Directory.CreateDirectory(dir);
        var full = Path.Combine(dir, name);
        await using (var fs = new FileStream(full, FileMode.CreateNew, FileAccess.Write))
        {
            await file.CopyToAsync(fs);
        }
        return (true, $"{PublicPrefix}/{folder}/{name}", null);
    }

    public Task DeleteAsync(string? publicPath)
    {
        if (string.IsNullOrWhiteSpace(publicPath) || !publicPath.StartsWith(PublicPrefix + "/")) return Task.CompletedTask;
        var relative = publicPath.Substring(PublicPrefix.Length + 1).Replace('/', Path.DirectorySeparatorChar);
        var full = Path.GetFullPath(Path.Combine(RootPath, relative));
        var root = Path.GetFullPath(RootPath) + Path.DirectorySeparatorChar;
        if (full.StartsWith(root, StringComparison.Ordinal) && File.Exists(full))
        {
            try { File.Delete(full); } catch (IOException) { /* arquivo em uso: ignora */ }
        }
        return Task.CompletedTask;
    }
}
