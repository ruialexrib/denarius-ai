namespace DenariusAI.Web.ViewModels;

/// <summary>
/// Describes one section of a Help Center documentation page.
/// </summary>
/// <param name="Id">Stable anchor identifier used for in-page navigation.</param>
/// <param name="Title">User-facing section title.</param>
/// <param name="Description">Short explanation of the section purpose.</param>
/// <param name="Items">Documentation items displayed in the section.</param>
public sealed record HelpSectionViewModel(string Id, string Title, string Description, IReadOnlyList<string> Items);

/// <summary>
/// Describes a Help Center topic and its complete functional documentation.
/// </summary>
/// <param name="Id">Stable topic identifier used by the help route.</param>
/// <param name="Category">Logical Help Center category.</param>
/// <param name="Icon">Small visual marker displayed on the topic card.</param>
/// <param name="Title">User-facing topic title.</param>
/// <param name="Subtitle">Short summary of the documented area.</param>
/// <param name="Controller">Application controller linked from the documentation page.</param>
/// <param name="Action">Application action linked from the documentation page.</param>
/// <param name="ActionLabel">Label for the direct application link.</param>
/// <param name="AdministratorOnly">Whether the topic must only be visible to administrators.</param>
/// <param name="Featured">Whether the topic should use the primary Help Center card treatment.</param>
/// <param name="AiTopic">Whether the topic is primarily related to AI-assisted functionality.</param>
/// <param name="Sections">Ordered documentation sections.</param>
public sealed record HelpDetailViewModel(
    string Id,
    string Category,
    string Icon,
    string Title,
    string Subtitle,
    string Controller,
    string Action,
    string ActionLabel,
    bool AdministratorOnly,
    bool Featured,
    bool AiTopic,
    IReadOnlyList<HelpSectionViewModel> Sections);

/// <summary>
/// Provides the Help Center index model for the current authenticated user.
/// </summary>
/// <param name="Topics">Help topics visible to the current user.</param>
public sealed record HelpIndexViewModel(IReadOnlyList<HelpDetailViewModel> Topics);
