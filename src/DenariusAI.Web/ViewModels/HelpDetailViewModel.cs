namespace DenariusAI.Web.ViewModels;

/// <summary>
/// Represents the HelpDetailViewModel type.
/// </summary>
public sealed record HelpSectionViewModel(string Title, string Description, IReadOnlyList<string> Items)
{
    public HelpSectionViewModel(string title, IReadOnlyList<string> items)
        : this(title, Describe(title), items) { }

    private static string Describe(string title) => title switch
    {
        "Criar e editar" => "Procedimento recomendado para registar informação e manter os dados existentes atualizados.",
        "Editar e eliminar" => "Cuidados a observar antes de alterar ou remover informação com impacto no histórico.",
        "Filtros" => "Use os filtros para reduzir os resultados apresentados sem alterar os dados guardados.",
        "Regras" => "Validações aplicadas pela aplicação para proteger a coerência contabilística e o histórico.",
        "Cálculos" => "Valores derivados automaticamente pela aplicação a partir dos dados introduzidos.",
        "Exemplo" => "Exemplo prático que pode adaptar à sua situação.",
        "Resolver problemas" => "Verificações úteis quando o resultado apresentado não é o esperado.",
        _ => $"Orientações práticas sobre {title.ToLowerInvariant()} nesta área."
    };
}
/// <summary>
/// Represents the HelpDetailViewModel type.
/// </summary>
public sealed record HelpDetailViewModel(string Id, string Title, string Subtitle, string Controller, string Action,
    string ActionLabel, IReadOnlyList<HelpSectionViewModel> Sections);
