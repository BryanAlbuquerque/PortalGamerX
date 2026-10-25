using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace PortalGamerX.Infrastructure;

/// <summary>Aceita "1.234,56", "1234,56" e "1234.56" independentemente da cultura do servidor.</summary>
public class FlexibleDecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var t = context.Metadata.ModelType;
        return t == typeof(decimal) || t == typeof(decimal?) ? new FlexibleDecimalModelBinder() : null;
    }
}

public class FlexibleDecimalModelBinder : IModelBinder
{
    public static bool TryParse(string? raw, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var s = raw.Trim().Replace("R$", "").Replace(" ", "");
        if (s.Contains(','))
            s = s.Replace(".", "").Replace(',', '.');
        return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var result = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (result == ValueProviderResult.None) return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, result);
        var raw = result.FirstValue;
        var nullable = Nullable.GetUnderlyingType(bindingContext.ModelType) != null;

        if (string.IsNullOrWhiteSpace(raw))
        {
            if (nullable) bindingContext.Result = ModelBindingResult.Success(null);
            else bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Informe um valor.");
            return Task.CompletedTask;
        }

        if (TryParse(raw, out var value)) bindingContext.Result = ModelBindingResult.Success(value);
        else bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Valor numérico inválido.");
        return Task.CompletedTask;
    }
}
