namespace PortalGamerX.Models.Entities
{
    public class Imagem
    {
        public Guid Id { get; set; }

        public int JogoId { get; set; }

        public Jogo Jogo { get; set; } = null!;

        public string UrlImagem { get; set; } = string.Empty;

    }
}
