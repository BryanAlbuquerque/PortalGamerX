using System.ComponentModel.DataAnnotations;

namespace EcommerceBase.Web.ViewModels;

public class AddressInput
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Informe um nome para o endereço."), StringLength(50)]
    [Display(Name = "Identificação (ex.: Casa)")]
    public string Label { get; set; } = "Casa";

    [Required(ErrorMessage = "Informe o destinatário."), StringLength(150)]
    [Display(Name = "Destinatário")]
    public string RecipientName { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Telefone")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Informe o CEP."), StringLength(9)]
    [Display(Name = "CEP")]
    public string PostalCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o logradouro."), StringLength(200)]
    [Display(Name = "Logradouro")]
    public string Street { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o número."), StringLength(20)]
    [Display(Name = "Número")]
    public string Number { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Complemento")]
    public string? Complement { get; set; }

    [Required(ErrorMessage = "Informe o bairro."), StringLength(100)]
    [Display(Name = "Bairro")]
    public string District { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a cidade."), StringLength(100)]
    [Display(Name = "Cidade")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o estado (UF)."), StringLength(2, MinimumLength = 2, ErrorMessage = "Use a sigla da UF com 2 letras.")]
    [Display(Name = "UF")]
    public string State { get; set; } = string.Empty;

    [Display(Name = "Definir como endereço principal")]
    public bool IsDefault { get; set; }

    public string? ReturnUrl { get; set; }
}
