using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace PortalGamerX.Models.Entities
{
    public class Cliente : IdentityUser<int>
    {
        [MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Sobrenome { get; set; } = string.Empty;

        [MaxLength(14)]
        public string? CPF { get; set; }

        public int Telefone { get; set; }

        public DateTime? DataNascimento { get; set; }

        public bool AceitouTermosUso { get; set; }

        public DateTime? DataAceiteTermos { get; set; }

        // Relacionamentos

        public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();

        public ICollection<Carrinho> Carrinho { get; set; } = new List<Carrinho>();

        public ICollection<Favorito> Favoritos { get; set; } = new List<Favorito>();

        public ICollection<HistoricoCompra> HistoricoCompras { get; set; } = new List<HistoricoCompra>();
    }
}