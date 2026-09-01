using System.Security.Claims;
using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;
using DenariusAI.Infrastructure.Persistence;
using DenariusAI.Web.Controllers;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.IntegrationTests;

/// <summary>Verifies insurance HTTP workflows against the application persistence model.</summary>
public sealed class InsuranceControllerTests
{
    /// <summary>Verifies filtering and pagination do not alter the unfiltered portfolio totals.</summary>
    [Fact]
    public async Task IndexFiltersAndPaginatesWhileKeepingPortfolioTotals()
    {
        await using var context = CreateContext();
        var motor = NewPolicy("Automóvel", "Lusitana", "AUTO-1", InsurancePolicyType.Motor);
        var home = NewPolicy("Casa", "Atlântica", "HOME-1", InsurancePolicyType.Home);
        var archived = NewPolicy("Viagem", "Global", "TRAVEL-1", InsurancePolicyType.Other);
        archived.Archive();
        context.AddRange(motor, home, archived);
        context.InsurancePremiums.Add(new InsurancePremium(motor.Id, 240m, new DateOnly(DateTime.Today.Year, 1, 1),
            new DateOnly(DateTime.Today.Year, 12, 31), DateOnly.FromDateTime(DateTime.Today).AddDays(-1)));
        await context.SaveChangesAsync();

        var result = Assert.IsType<ViewResult>(await CreateController(context).Index("a", type: null, status: InsurancePolicyStatus.Active,
            page: 2, pageSize: 1, cancellationToken: CancellationToken.None));
        var model = Assert.IsType<InsurancePortfolioViewModel>(result.Model);

        Assert.Equal(2, model.ActivePolicies);
        Assert.Equal(240m, model.AnnualCost);
        Assert.Equal(1, model.OutstandingPremiums);
        Assert.Equal(2, model.Pagination.TotalItems);
        Assert.Equal(1, model.Pagination.Page);
        Assert.Equal(10, model.Pagination.PageSize);
        Assert.Equal(2, model.Policies.Count);
        Assert.Contains(model.Policies, policy => policy.Name == "Casa");
    }

    /// <summary>Verifies policy lifecycle endpoints persist status and audit actor changes.</summary>
    [Fact]
    public async Task LifecycleActionsPersistStatusAndActor()
    {
        await using var context = CreateContext();
        var policy = NewPolicy("Saúde", "Saudável", "HEALTH-1", InsurancePolicyType.Health);
        context.Add(policy);
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        Assert.IsType<RedirectToActionResult>(await controller.Archive(policy.Id, CancellationToken.None));
        Assert.Equal(InsurancePolicyStatus.Archived, policy.Status);
        Assert.Equal("test-user", policy.UpdatedBy);
        Assert.IsType<RedirectToActionResult>(await controller.Activate(policy.Id, CancellationToken.None));
        Assert.Equal(InsurancePolicyStatus.Active, policy.Status);
        Assert.IsType<RedirectToActionResult>(await controller.Cancel(policy.Id, CancellationToken.None));
        Assert.Equal(InsurancePolicyStatus.Cancelled, policy.Status);
    }

    /// <summary>Verifies premiums can only be created for active policies.</summary>
    [Fact]
    public async Task AddPremiumRejectsInactivePolicyAndPersistsForActivePolicy()
    {
        await using var context = CreateContext();
        var policy = NewPolicy("Casa", "Atlântica", "HOME-2", InsurancePolicyType.Home);
        policy.Archive();
        context.Add(policy);
        await context.SaveChangesAsync();
        var controller = CreateController(context);
        var model = new InsurancePremiumFormViewModel
        {
            Amount = 120m,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 12, 31),
            DueDate = new DateOnly(2026, 1, 15),
            Reference = "ANUAL"
        };

        await controller.AddPremium(policy.Id, model, CancellationToken.None);
        Assert.Empty(context.InsurancePremiums);
        Assert.Equal("Apenas apólices ativas podem receber novos prémios.", controller.TempData["ErrorMessage"]);
        policy.Activate();
        await context.SaveChangesAsync();
        await controller.AddPremium(policy.Id, model, CancellationToken.None);

        var premium = await context.InsurancePremiums.SingleAsync();
        Assert.Equal(120m, premium.Amount);
        Assert.Equal("ANUAL", premium.Reference);
        Assert.Equal("test-user", premium.CreatedBy);
    }

    /// <summary>Verifies general policy uploads reject disguised files and persist valid PDFs without a premium.</summary>
    [Fact]
    public async Task UploadPolicyAttachmentValidatesSignatureAndDoesNotRequirePremium()
    {
        await using var context = CreateContext();
        var policy = NewPolicy("Automóvel", "Lusitana", "AUTO-2", InsurancePolicyType.Motor);
        context.Add(policy);
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        await controller.UploadPolicyAttachment(policy.Id, CreateFile("not-pdf", "falso.pdf"), CancellationToken.None);
        Assert.Empty(context.InsurancePolicyAttachments);
        Assert.Equal("Seleciona um ficheiro PDF válido com até 5 MB.", controller.TempData["ErrorMessage"]);
        var result = await controller.UploadPolicyAttachment(policy.Id, CreateFile("%PDF-1.7 content", "condicoes.pdf"), CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        var attachment = await context.InsurancePolicyAttachments.SingleAsync();
        Assert.Equal("condicoes.pdf", attachment.FileName);
        Assert.Equal("test-user", attachment.CreatedBy);
        Assert.Empty(context.InsurancePremiums);
    }

    /// <summary>Creates an isolated in-memory application database.</summary>
    /// <returns>The test database context.</returns>
    private static DenariusDbContext CreateContext() => new(new DbContextOptionsBuilder<DenariusDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    /// <summary>Creates an authenticated controller with functional temporary data.</summary>
    /// <param name="context">Test database context.</param>
    /// <returns>The configured controller.</returns>
    private static InsuranceController CreateController(DenariusDbContext context)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "test-user")], "Test"))
        };
        return new InsuranceController(context)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };
    }

    /// <summary>Creates a representative insurance policy.</summary>
    /// <param name="name">Policy name.</param>
    /// <param name="insurer">Insurer name.</param>
    /// <param name="number">Policy number.</param>
    /// <param name="type">Insurance type.</param>
    /// <returns>The new policy.</returns>
    private static InsurancePolicy NewPolicy(string name, string insurer, string number, InsurancePolicyType type) =>
        new(name, insurer, number, type, InsurancePaymentFrequency.Annual, new DateOnly(2026, 1, 1));

    /// <summary>Creates an uploaded PDF test file.</summary>
    /// <param name="content">File content.</param>
    /// <param name="fileName">Client file name.</param>
    /// <returns>The uploaded file abstraction.</returns>
    private static IFormFile CreateFile(string content, string fileName)
    {
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        return new FormFile(stream, 0, stream.Length, "file", fileName) { Headers = new HeaderDictionary(), ContentType = "application/pdf" };
    }

    /// <summary>Stores temporary controller messages in memory for a single test.</summary>
    private sealed class TestTempDataProvider : ITempDataProvider
    {
        private Dictionary<string, object> data = [];

        /// <summary>Loads the current temporary data values.</summary>
        /// <param name="context">HTTP context.</param>
        /// <returns>The stored values.</returns>
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>(data);

        /// <summary>Saves temporary data values.</summary>
        /// <param name="context">HTTP context.</param>
        /// <param name="values">Values to save.</param>
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) => data = new Dictionary<string, object>(values);
    }
}
