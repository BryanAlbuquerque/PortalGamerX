using System.ComponentModel.DataAnnotations;

namespace EcommerceBase.Web.ViewModels;

public class LoginVm
{
    [Required(ErrorMessage = "Informe o e-mail."), EmailAddress(ErrorMessage = "E-mail inválido.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha."), DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Manter conectado")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

/// <summary>Sem campo de papel: o cadastro público SEMPRE cria Customer (atribuído no servidor).</summary>
public class RegisterVm
{
    [Required(ErrorMessage = "Informe seu nome."), StringLength(150, MinimumLength = 3, ErrorMessage = "Informe o nome completo.")]
    [Display(Name = "Nome completo")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail."), EmailAddress(ErrorMessage = "E-mail inválido."), StringLength(256)]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "CPF ou CNPJ (opcional)")]
    public string? Document { get; set; }

    [StringLength(20)]
    [Display(Name = "Telefone")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Informe a senha."), DataType(DataType.Password), StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter ao menos 8 caracteres.")]
    [Display(Name = "Senha")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme a senha."), DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "As senhas não conferem.")]
    [Display(Name = "Confirmar senha")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Range(typeof(bool), "true", "true", ErrorMessage = "É necessário aceitar os termos.")]
    [Display(Name = "Li e aceito os termos e a política de privacidade")]
    public bool AcceptTerms { get; set; }
}

public class ForgotPasswordVm
{
    [Required(ErrorMessage = "Informe o e-mail."), EmailAddress(ErrorMessage = "E-mail inválido.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordVm
{
    [Required] public string UserId { get; set; } = string.Empty;
    [Required] public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a nova senha."), DataType(DataType.Password), StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter ao menos 8 caracteres.")]
    [Display(Name = "Nova senha")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme a senha."), DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "As senhas não conferem.")]
    [Display(Name = "Confirmar nova senha")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ChangePasswordVm
{
    [Required(ErrorMessage = "Informe a senha atual."), DataType(DataType.Password)]
    [Display(Name = "Senha atual")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a nova senha."), DataType(DataType.Password), StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter ao menos 8 caracteres.")]
    [Display(Name = "Nova senha")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme a nova senha."), DataType(DataType.Password), Compare(nameof(NewPassword), ErrorMessage = "As senhas não conferem.")]
    [Display(Name = "Confirmar nova senha")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>Perfil do cliente: apenas dados pessoais. Papel, e-mail e status NÃO são editáveis aqui.</summary>
public class ProfileVm
{
    [Required(ErrorMessage = "Informe seu nome."), StringLength(150, MinimumLength = 3)]
    [Display(Name = "Nome completo")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Telefone")]
    public string? Phone { get; set; }

    [StringLength(20)]
    [Display(Name = "CPF ou CNPJ")]
    public string? Document { get; set; }
}

public class ErrorVm
{
    public int Code { get; set; } = 500;
    public string Title { get; set; } = "Ocorreu um erro";
    public string Message { get; set; } = "Não foi possível concluir a operação.";
}

public class TextPageVm
{
    public string Title { get; set; } = string.Empty;
    public string? Text { get; set; }
}
