namespace PortalGamerX.Models.Entities
{
    public class Favorito
    {

        public int Id { get; set; }
        public string UsuarioId { get; set; } = string.Empty;

        public ApplicationUser Usuario { get; set; } = null!;

        public int JogoId { get; set; }

        public Jogo Jogo { get; set; } = null!;

        public DateTime DataInclusao { get; set; } = DateTime.Now;
    }
}
