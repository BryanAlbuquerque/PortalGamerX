using System.Linq.Expressions;
using EcommerceBase.Web.Data;
using EcommerceBase.Web.Domain;
using EcommerceBase.Web.Domain.Entities;
using EcommerceBase.Web.Infrastructure;
using EcommerceBase.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace EcommerceBase.Web.Services;

public interface ICatalogService
{
    Task<HomeVm> GetHomeAsync();
    Task<ProductListVm> SearchAsync(ProductQuery query);
    Task<ProductDetailsVm?> GetDetailsAsync(string slug);
}

public class CatalogService : ICatalogService
{
    // Preço efetivo no banco: promocional válido (>0 e menor que o preço) ou o preço normal.
    private static readonly Expression<Func<Product, decimal>> EffectivePriceExpr =
        p => p.PromotionalPrice != null && p.PromotionalPrice > 0 && p.PromotionalPrice < p.Price ? p.PromotionalPrice.Value : p.Price;

    private static readonly OrderStatus[] SoldStatuses =
        { OrderStatus.Paid, OrderStatus.Processing, OrderStatus.Shipped, OrderStatus.Delivered };

    private readonly AppDbContext _db;
    public CatalogService(AppDbContext db) => _db = db;

    private IQueryable<Product> Visible() =>
        _db.Products.AsNoTracking().Where(p => p.IsActive && p.Category!.IsActive);

    private static IQueryable<ProductCardVm> ToCards(IQueryable<Product> q) =>
        q.Select(p => new ProductCardVm
        {
            Id = p.Id,
            Name = p.Name,
            Slug = p.Slug,
            Price = p.Price,
            EffectivePrice = p.PromotionalPrice != null && p.PromotionalPrice > 0 && p.PromotionalPrice < p.Price ? p.PromotionalPrice.Value : p.Price,
            IsPurchasable = p.IsAvailable && p.StockQuantity > 0,
            CategoryName = p.Category!.Name,
            ImagePath = p.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder).Select(i => i.Path).FirstOrDefault()
        });

    public async Task<HomeVm> GetHomeAsync()
    {
        var now = DateTime.UtcNow;
        var vm = new HomeVm
        {
            Banners = await _db.Banners.AsNoTracking()
                .Where(b => b.IsActive && (b.StartsAtUtc == null || b.StartsAtUtc <= now) && (b.EndsAtUtc == null || b.EndsAtUtc >= now))
                .OrderBy(b => b.SortOrder).ThenBy(b => b.Id).Take(8).ToListAsync(),
            Categories = await _db.Categories.AsNoTracking()
                .Where(c => c.IsActive && c.ParentId == null).OrderBy(c => c.SortOrder).ThenBy(c => c.Name).Take(12).ToListAsync(),
            Featured = await ToCards(Visible().Where(p => p.IsFeatured).OrderByDescending(p => p.UpdatedAtUtc).Take(8)).ToListAsync(),
            Newest = await ToCards(Visible().OrderByDescending(p => p.CreatedAtUtc).Take(8)).ToListAsync(),
            Offers = await ToCards(Visible().Where(p => p.PromotionalPrice != null && p.PromotionalPrice > 0 && p.PromotionalPrice < p.Price)
                .OrderByDescending(p => p.UpdatedAtUtc).Take(8)).ToListAsync()
        };

        var topIds = await _db.OrderItems
            .Where(i => SoldStatuses.Contains(i.Order!.Status))
            .GroupBy(i => i.ProductId)
            .Select(g => new { Id = g.Key, Qty = g.Sum(x => x.Quantity) })
            .OrderByDescending(x => x.Qty).Take(8).Select(x => x.Id).ToListAsync();
        if (topIds.Count > 0)
        {
            var cards = await ToCards(Visible().Where(p => topIds.Contains(p.Id))).ToListAsync();
            vm.BestSellers = topIds.Select(id => cards.FirstOrDefault(c => c.Id == id)).Where(c => c != null).Select(c => c!).ToList();
        }
        return vm;
    }

    public async Task<ProductListVm> SearchAsync(ProductQuery query)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 4, 48);
        var q = Visible();

        var vm = new ProductListVm { Query = query };
        vm.Categories = await _db.Categories.AsNoTracking().Where(c => c.IsActive && c.ParentId == null)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync();

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var cat = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Slug == query.Category && c.IsActive);
            if (cat != null)
            {
                vm.CurrentCategory = cat;
                var ids = await _db.Categories.Where(c => c.Id == cat.Id || c.ParentId == cat.Id).Select(c => c.Id).ToListAsync();
                q = q.Where(p => ids.Contains(p.CategoryId));
            }
            else q = q.Where(p => false);
        }

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            foreach (var term in query.Q.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(5))
            {
                var pattern = "%" + term.EscapeLike() + "%";
                q = q.Where(p => EF.Functions.ILike(p.Name, pattern, "\\")
                                 || (p.ShortDescription != null && EF.Functions.ILike(p.ShortDescription, pattern, "\\"))
                                 || (p.Description != null && EF.Functions.ILike(p.Description, pattern, "\\"))
                                 || EF.Functions.ILike(p.Sku, pattern, "\\"));
            }
        }

        if (query.MinPrice.HasValue)
        {
            var min = query.MinPrice.Value;
            q = q.Where(p => (p.PromotionalPrice != null && p.PromotionalPrice > 0 && p.PromotionalPrice < p.Price ? p.PromotionalPrice.Value : p.Price) >= min);
        }
        if (query.MaxPrice.HasValue)
        {
            var max = query.MaxPrice.Value;
            q = q.Where(p => (p.PromotionalPrice != null && p.PromotionalPrice > 0 && p.PromotionalPrice < p.Price ? p.PromotionalPrice.Value : p.Price) <= max);
        }

        var total = await q.CountAsync();
        q = query.Sort switch
        {
            "price_asc" => q.OrderBy(EffectivePriceExpr).ThenBy(p => p.Id),
            "price_desc" => q.OrderByDescending(EffectivePriceExpr).ThenBy(p => p.Id),
            "name_desc" => q.OrderByDescending(p => p.Name).ThenBy(p => p.Id),
            "newest" => q.OrderByDescending(p => p.CreatedAtUtc).ThenBy(p => p.Id),
            _ => q.OrderBy(p => p.Name).ThenBy(p => p.Id)
        };

        vm.Result = new PagedResult<ProductCardVm>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = await ToCards(q.Skip((page - 1) * pageSize).Take(pageSize)).ToListAsync()
        };
        return vm;
    }

    public async Task<ProductDetailsVm?> GetDetailsAsync(string slug)
    {
        var product = await _db.Products.AsNoTracking()
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive && p.Category!.IsActive);
        if (product == null) return null;

        var related = await ToCards(Visible().Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id)
            .OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.CreatedAtUtc).Take(4)).ToListAsync();
        return new ProductDetailsVm { Product = product, Related = related };
    }
}
