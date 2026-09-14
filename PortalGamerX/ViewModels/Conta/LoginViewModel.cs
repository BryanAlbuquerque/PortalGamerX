using System.ComponentModel.DataAnnotations;

namespace PortalGamerX.ViewModels.Conta
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Informe o Email")]
        [EmailAddress(ErrorMessage = "Informe um email válido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Informe a Senha")]
        [DataType(DataType.Password)]
        public string Senha { get; set; }
        
        [Display(Name = "Lembrar-me")]
        public bool LembrarMe { get; set; }
    }
}
