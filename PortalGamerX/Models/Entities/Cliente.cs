using System.ComponentModel.DataAnnotations;

namespace PortalGamerX.Models.Entities
{
    public class Cliente
    {
        public int Id { get; set; }

        [MaxLength(100)]
        public string Nome { get; set; }
        
        [MaxLength(100)]
        public string Sobrenome { get; set; }

        [MaxLength(14)]
        public string? CPF { get; set; }

        [MaxLength(50)]
        public string Email { get; set; }
        public DateTime? DataNascimento { get; set; }

        public string? Telefone { get; set; } = null;

        public bool AceitouTermosUso { get; set; }

        public DateTime? DataAceiteTermos { get; set; }

        //Relacionamentos

        public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();

        public ICollection<Carrinho> Carrinho { get; set; } = new List<Carrinho>();

        public ICollection<Favorito> Favoritos { get; set; } = new List<Favorito>();

        public ICollection<HistoricoCompra> HistoricoCompras { get; set; } = new List<HistoricoCompra>();
    }
}
