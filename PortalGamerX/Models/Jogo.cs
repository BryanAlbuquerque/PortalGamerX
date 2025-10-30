using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PortalGamerX.Models
{
    [Table("Jogos")]
    public class Jogo
    {
        [Key]
        public int JogoId { get; set; }

        [Required(ErrorMessage = "Informe o nome do Jogo")]
        [Display(Name = "Nome do Jogo")]
        [StringLength(80, MinimumLength = 5, ErrorMessage = "O {0} Deve ter o minimo {1} e no maximo {2} Caracteres")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "Informe a data")]
        [DataType(DataType.Date)]
        [Display(Name = "Data do Lançamento")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime DataLancamento { get; set; }

        [Required(ErrorMessage = "Informe a Descrição do Jogo")]
        [Display(Name = "Descrição do Jogo")]
        [StringLength(300, MinimumLength = 10, ErrorMessage = "A {0} Deve ter o minimo {1} e no maximo {2} Caracteres")]
        public string Descricao { get; set; }

        [Required(ErrorMessage = "Informe o Preço do Produto")]
        [Display(Name = "Preço")]
        [Column(TypeName = "decimal(10,2)")]
        [Range(1, 999.99, ErrorMessage = "O preço deve estar entre 1 e 999,99")]
        public decimal Preco { get; set; }

        [Display(Name = "Caminho da Imagem")]
        public string ImagemUrl { get; set; }

        [Display(Name = "Estoque")]
        public bool EmEstoque { get; set; }


        // foreign Key e relacionamentos
        public int CategoriaId { get; set; }
        public virtual Categoria Categoria { get; set; }
    }
}
