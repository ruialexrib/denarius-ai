using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DenariusAI.Web.ModelBinding;

/// <summary>Reads decimal values submitted by HTML number inputs using their culture-independent wire format.</summary>
public sealed class HtmlNumberModelBinder : IModelBinder
{
    /// <summary>Binds one invariant decimal without accepting culture-specific thousands separators or empty values.</summary>
    /// <param name="bindingContext">The MVC context containing the submitted field and validation state.</param>
    /// <returns>A completed task after setting the parsed value or a validation error.</returns>
    /// <exception cref="ArgumentNullException">The binding context is null.</exception>
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);
        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (value == ValueProviderResult.None) return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, value);
        const NumberStyles styles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;
        if (value.Length == 1 && decimal.TryParse(value.FirstValue, styles, CultureInfo.InvariantCulture, out var amount))
        {
            bindingContext.Result = ModelBindingResult.Success(amount);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Indique um valor orçamentado válido.");
            bindingContext.Result = ModelBindingResult.Failed();
        }

        return Task.CompletedTask;
    }
}
