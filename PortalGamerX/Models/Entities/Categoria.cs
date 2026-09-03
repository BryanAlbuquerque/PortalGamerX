using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PortalGamerX.Models.Entities
{
    [Table("Categorias")]
    public class Categoria
    {
        [Key]
        public int CategoriaId { get; set; }

        public int JogoId { get; set; }


        [StringLength(100, ErrorMessage = "Tamanho maximo permitido é 100 Caracteres!")]
        [Required(ErrorMessage = "Informe o nome da categoria")]
        [Display(Name = "Nome")]
        public string Nome { get; set; }

        [StringLength(200, ErrorMessage = "Tamanho maximo permitido é 200 Caracteres!")]
        [Required(ErrorMessage = "Informe a descrição da categoria")]
        [Display(Name = "Descrição")]
        public string Descricao { get; set; }

        [Required(ErrorMessage = "Informe o tipo da categoria")]
        [StringLength(50, ErrorMessage = "Tamanho maximo permitido é 50 Caracteres!")]
        [Display(Name = "Tipo da Categoria")]
        public string TipoCategoria { get; set; }

        public List<Jogo> Jogos { get; set; }
    }
}
