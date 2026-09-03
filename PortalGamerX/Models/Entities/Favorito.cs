namespace PortalGamerX.Models.Entities
{
    public class Favorito
    {
        public string UsuarioId { get; set; } = string.Empty;

        public Cliente Usuario { get; set; } = null!;

        public int JogoId { get; set; }

        public Jogo Jogo { get; set; } = null!;

        public DateTime DataInclusao { get; set; } = DateTime.Now;
    }
}
