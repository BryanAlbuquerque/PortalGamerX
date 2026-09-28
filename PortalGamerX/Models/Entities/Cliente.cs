using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace PortalGamerX.Models.Entities
{
    public class Cliente : IdentityUser<int>
    {
        [Required]
        [MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Sobrenome { get; set; } = string.Empty;

        [MaxLength(14)]
        public string? CPF { get; set; }

        [MaxLength(20)]
        public string? Telefone { get; set; }

        public DateTime? DataNascimento { get; set; }

        public bool AceitouTermosUso { get; set; }

        public DateTime? DataAceiteTermos { get; set; }

        public ICollection<Pedido> Pedidos { get; set; }
            = new List<Pedido>();

        public ICollection<Carrinho> Carrinho { get; set; }
            = new List<Carrinho>();

        public ICollection<Favorito> Favoritos { get; set; }
            = new List<Favorito>();

        public ICollection<HistoricoCompra> HistoricoCompras { get; set; }
            = new List<HistoricoCompra>();
    }
}