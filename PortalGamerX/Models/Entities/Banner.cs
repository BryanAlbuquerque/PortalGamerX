namespace PortalGamerX.Models.Entities
{
    public class Banner
    {
        public Guid Id { get; set; }

        public string Titulo { get; set; } = string.Empty;

        public string? LinkDestino { get; set; }

        public string Imagem { get; set; } = string.Empty;

        public bool Ativo { get; set; } = true;

        public int Ordem { get; set; }

    }
}
