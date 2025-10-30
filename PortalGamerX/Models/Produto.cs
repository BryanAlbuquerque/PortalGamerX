using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PortalGamerX.Models
{
    [Table("Produtos")]
    public class Produto
    {
        [Key]
        public int ProdutoId { get; set; }

        [StringLength(80, MinimumLength = 5, ErrorMessage = "O {0} Deve ter o minimo {1} e no maximo {2} Caracteres")]
        [Required(ErrorMessage = "Informe o nome do Produto")]
        [Display(Name = "Nome do Produto")]
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public decimal Preco { get; set; }
        public string ImagemUrl { get; set; }
        public bool EmEstoque { get; set; }

        public int CategoriaId { get; set; }
        public virtual Categoria Categoria { get; set; }
    }
}
