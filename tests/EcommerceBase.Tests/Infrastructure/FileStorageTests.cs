using EcommerceBase.Web.Infrastructure;

namespace EcommerceBase.Tests.Infrastructure;

public class FileStorageTests
{
    [Fact] public void Jpeg() => Assert.Equal(".jpg", LocalFileStorage.DetectExtension(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0 }, false));
    [Fact] public void Png() => Assert.Equal(".png", LocalFileStorage.DetectExtension(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0 }, false));
    [Fact] public void Gif() => Assert.Equal(".gif", LocalFileStorage.DetectExtension("GIF89a"u8.ToArray(), false));
    [Fact] public void Webp() => Assert.Equal(".webp", LocalFileStorage.DetectExtension("RIFF\0\0\0\0WEBPVP8 "u8.ToArray(), false));

    [Fact]
    public void Ico_so_quando_permitido()
    {
        var ico = new byte[] { 0, 0, 1, 0, 1, 0 };
        Assert.Null(LocalFileStorage.DetectExtension(ico, false));
        Assert.Equal(".ico", LocalFileStorage.DetectExtension(ico, true));
    }

    [Fact]
    public void Svg_html_e_executaveis_sao_rejeitados()
    {
        Assert.Null(LocalFileStorage.DetectExtension("<svg xmlns=\"http://www.w3.org/2000/svg\"/>"u8.ToArray(), true));
        Assert.Null(LocalFileStorage.DetectExtension("<html><script>"u8.ToArray(), true));
        Assert.Null(LocalFileStorage.DetectExtension("MZ\x90\0\x03\0\0\0"u8.ToArray(), true));
        Assert.Null(LocalFileStorage.DetectExtension(ReadOnlySpan<byte>.Empty, true));
    }
}
