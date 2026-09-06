using DenariusAI.Infrastructure.Persistence;
using DenariusAI.Web.Controllers;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DenariusAI.IntegrationTests;

/// <summary>Verifies administrative audit controller query and navigation behavior.</summary>
public sealed class AuditControllerCoverageTests
{
    /// <summary>Verifies an empty audit store returns a valid paginated view for invalid optional filters.</summary>
    [Fact]
    public async Task IndexReturnsEmptyModelAndNormalizesInvalidAction()
    {
        await using var context = ControllerTestSupport.CreateContext();
        var controller = ControllerTestSupport.Configure(new AuditController(context));

        var result = Assert.IsType<ViewResult>(await controller.Index(
            search: "missing",
            entityType: null,
            recordId: null,
            action: "Invalid",
            from: new DateOnly(2026, 1, 1),
            to: new DateOnly(2026, 12, 31),
            page: 0,
            pageSize: 0,
            cancellationToken: CancellationToken.None));
        var model = Assert.IsType<AuditIndexViewModel>(result.Model);

        Assert.Empty(model.Items);
        Assert.Null(model.Action);
        Assert.Empty(model.EntityTypes.Skip(1));
        Assert.Equal(1, model.Pagination.Page);
        Assert.True(model.Pagination.PageSize > 0);
    }

    /// <summary>Verifies details returns not found when an audit identifier does not exist.</summary>
    [Fact]
    public async Task DetailsReturnsNotFoundForMissingAuditEntry()
    {
        await using var context = ControllerTestSupport.CreateContext();
        var controller = ControllerTestSupport.Configure(new AuditController(context));

        Assert.IsType<NotFoundResult>(await controller.Details(Guid.NewGuid(), CancellationToken.None));
    }

    /// <summary>Verifies record navigation redirects to the audit list while preserving entity filters.</summary>
    [Fact]
    public void RecordRedirectsWithEntityFilters()
    {
        using DenariusDbContext context = ControllerTestSupport.CreateContext();
        var controller = ControllerTestSupport.Configure(new AuditController(context));

        var result = Assert.IsType<RedirectToActionResult>(controller.Record("JournalEntry", "entry-42"));

        Assert.Equal(nameof(AuditController.Index), result.ActionName);
        Assert.Equal("JournalEntry", result.RouteValues!["entityType"]);
        Assert.Equal("entry-42", result.RouteValues["recordId"]);
    }
}
