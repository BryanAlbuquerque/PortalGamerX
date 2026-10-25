using System.ComponentModel.DataAnnotations;
using PortalGamerX.Domain.Entities;

namespace PortalGamerX.ViewModels;

public class CheckoutVm
{
    public CartView Cart { get; set; } = new();
    public List<Address> Addresses { get; set; } = new();
    public bool PaymentEnabled { get; set; }
    public Guid IdempotencyKey { get; set; } = Guid.NewGuid();
    public int? SelectedAddressId { get; set; }
    public string? Notes { get; set; }
}

public class CheckoutInput
{
    public Guid IdempotencyKey { get; set; }

    [Required(ErrorMessage = "Selecione o endereço de entrega.")]
    public int AddressId { get; set; }

    [Required(ErrorMessage = "Selecione a modalidade de frete.")]
    [StringLength(60)]
    public string ShippingServiceCode { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public class OrderListVm
{
    public List<Order> Orders { get; set; } = new();
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
}
