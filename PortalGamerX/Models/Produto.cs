using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PortalGamerX.Models
{
    [Table("Produtos")]
    public class Produto
    {
        [Key]
        public int ProdutoId { get; set; }

        [Required(ErrorMessage = "Informe o nome do Produto")]
        [StringLength(80, MinimumLength = 5, ErrorMessage = "O {0} Deve ter o minimo {1} e no maximo {2} Caracteres")]
        [Display(Name = "Nome do Produto")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "Informe a Descrição do Produto")]
        [StringLength(300, MinimumLength = 10, ErrorMessage = "A {0} Deve ter o minimo {1} e no maximo {2} Caracteres")]
        [Display(Name = "Descrição do Produto")]
        public string Descricao { get; set; }

        [Required(ErrorMessage = "Informe o Preço do Produto")]
        [Display(Name = "Preço")]
        [Column(TypeName = "decimal(10,2)")]
        [Range(1, 999.99, ErrorMessage = "O preço deve estar entre 1 e 999,99")]
        public decimal Preco { get; set; }

        [Required(ErrorMessage = "Informe a Quantidade do Produto")]
        [Display(Name = "Quantidade em Estoque")]
        public int Quantidade { get; set; }

        [Display(Name = "Caminho da Imagem")]
        [StringLength(200, ErrorMessage = "O {0} deve ter o maximo de {1} Caracteres!")]
        public string ImagemUrl { get; set; }

        // foreign Key e relacionamentos
        public int CategoriaId { get; set; }
        public virtual Categoria Categoria { get; set; }
    }
}
