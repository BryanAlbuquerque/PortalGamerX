using EcommerceBase.Web.Domain.Rules;
using EcommerceBase.Web.Integrations.Shipping;

namespace EcommerceBase.Web.Infrastructure;

public static class ShippingJson
{
    public static object From(ShippingQuoteResult q) => new
    {
        success = q.Success,
        error = q.Error,
        simulated = q.IsSimulated,
        options = q.Options.Select(o => new
        {
            code = o.Code,
            name = o.Name,
            carrier = o.Carrier,
            price = o.Price,
            priceText = o.Price == 0 ? "Grátis" : o.Price.ToBrl(),
            days = o.DeliveryDays,
            simulated = o.IsSimulated
        })
    };
}
