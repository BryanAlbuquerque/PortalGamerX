using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace EcommerceBase.Web.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>CPF ou CNPJ (somente dígitos), opcional.</summary>
    [MaxLength(20)]
    public string? Document { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAtUtc { get; set; }

    public List<Address> Addresses { get; set; } = new();
}

public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }
    public ApplicationRole(string name) : base(name) { }
}

public class Address
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }

    [MaxLength(50)] public string Label { get; set; } = "Casa";
    [MaxLength(150)] public string RecipientName { get; set; } = string.Empty;
    [MaxLength(20)] public string? Phone { get; set; }
    [MaxLength(8)] public string PostalCode { get; set; } = string.Empty;
    [MaxLength(200)] public string Street { get; set; } = string.Empty;
    [MaxLength(20)] public string Number { get; set; } = string.Empty;
    [MaxLength(100)] public string? Complement { get; set; }
    [MaxLength(100)] public string District { get; set; } = string.Empty;
    [MaxLength(100)] public string City { get; set; } = string.Empty;
    [MaxLength(2)] public string State { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
