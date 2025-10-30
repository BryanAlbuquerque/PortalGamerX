using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PortalGamerX.Models
{
    [Table("Jogos")]
    public class Jogo
    {
        public int JogoId { get; set; }
        public string Nome { get; set; }
        public DateTime DataLancamento { get; set; }
        public string Descricao { get; set; }
        public decimal Preco { get; set; }
        public string ImagemUrl { get; set; }
        public bool EmEstoque { get; set; }


        public int CategoriaId { get; set; }
        public virtual Categoria Categoria { get; set; }
    }
}
