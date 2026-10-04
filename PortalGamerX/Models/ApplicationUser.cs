using Microsoft.AspNetCore.Identity;
using PortalGamerX.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace PortalGamerX.Models
{
    public class ApplicationUser : IdentityUser
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

        public ICollection<Carrinho> Carrinhos { get; set; }
            = new List<Carrinho>();

        public ICollection<Favorito> Favoritos { get; set; }
            = new List<Favorito>();

        public ICollection<HistoricoCompra> HistoricosCompras { get; set; }
            = new List<HistoricoCompra>();
    }
}