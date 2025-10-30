using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PortalGamerX.Models
{
    [Table("Categoria")]
    public class Categoria
    {
        public int CategoriaId { get; set; }
        public string Nome { get; set; }
        public string Descricao { get; set; }

        public List<Produto> Produtos { get; set; }
        public List<Jogo> Jogos { get; set; }
    }
}
