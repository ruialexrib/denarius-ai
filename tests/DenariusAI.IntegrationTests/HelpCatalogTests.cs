using DenariusAI.Web.Models;

namespace DenariusAI.IntegrationTests;

/// <summary>
/// Verifies the Help Center catalogue and its authorization-aware discovery rules.
/// </summary>
public sealed class HelpCatalogTests
{
    /// <summary>
    /// Verifies that newly documented first-class functional areas are present in the catalogue.
    /// </summary>
    [Fact]
    public void Pages_Contain_Current_FirstClass_Functional_Areas()
    {
        var expected = new[] { "movimentos", "reconciliation", "budget", "stocks", "insurance", "warranties", "correspondence", "reminders", "analytics" };

        foreach (var id in expected)
        {
            Assert.True(HelpCatalog.Pages.ContainsKey(id), $"Missing Help Center topic: {id}");
            Assert.NotEmpty(HelpCatalog.Pages[id].Sections);
        }
    }

    /// <summary>
    /// Verifies that administrator documentation is hidden from ordinary users and retained for administrators.
    /// </summary>
    [Fact]
    public void VisiblePages_Filter_AdministratorOnly_Topics()
    {
        var ordinaryTopics = HelpCatalog.VisiblePages(false);
        var administratorTopics = HelpCatalog.VisiblePages(true);

        Assert.DoesNotContain(ordinaryTopics, topic => topic.AdministratorOnly);
        Assert.Contains(administratorTopics, topic => topic.Id == "settings" && topic.AdministratorOnly);
        Assert.Contains(administratorTopics, topic => topic.Id == "audit" && topic.AdministratorOnly);
        Assert.True(administratorTopics.Count > ordinaryTopics.Count);
    }

    /// <summary>
    /// Verifies that section anchors are populated and unique within every documentation page.
    /// </summary>
    [Fact]
    public void Pages_Have_Unique_Section_Anchors()
    {
        foreach (var page in HelpCatalog.Pages.Values)
        {
            Assert.All(page.Sections, section => Assert.False(string.IsNullOrWhiteSpace(section.Id)));
            Assert.Equal(page.Sections.Count, page.Sections.Select(section => section.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }
    }
}
