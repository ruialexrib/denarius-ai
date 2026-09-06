using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DenariusAI.Web.ModelBinding;

/// <summary>Binds an HTML number field from form data without greedily creating absent collection elements.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class HtmlNumberAttribute : ModelBinderAttribute
{
    /// <summary>Selects the invariant decimal binder for an HTML number input.</summary>
    public HtmlNumberAttribute() : base(typeof(HtmlNumberModelBinder)) { }

    /// <summary>Gets the form-only source so MVC checks field prefixes before binding collection elements.</summary>
    public override BindingSource BindingSource => BindingSource.Form;
}
