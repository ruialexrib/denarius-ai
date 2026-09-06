using System.Security.Claims;
using DenariusAI.Domain.Entities;
using DenariusAI.Infrastructure.Persistence;
using DenariusAI.Web.Controllers;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.IntegrationTests;

/// <summary>Verifies reminder controller workflows against the application persistence model.</summary>
public sealed class RemindersControllerTests
{
    /// <summary>Verifies reminder filtering preserves summary counts across active, scheduled, and acknowledged states.</summary>
    [Fact]
    public async Task IndexFiltersRowsAndKeepsGlobalSummaryCounts()
    {
        await using var context = CreateContext();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var active = new Reminder("Seguro automóvel", today.AddDays(2), 5);
        var scheduled = new Reminder("Renovar passaporte", today.AddDays(30), 5);
        var acknowledged = new Reminder("Rever orçamento", today, 1);
        context.AddRange(active, scheduled, acknowledged);
        context.ReminderAcknowledgements.Add(new ReminderAcknowledgement
        {
            ReminderId = acknowledged.Id,
            UserId = "test-user",
            AcknowledgedAt = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();

        var result = Assert.IsType<ViewResult>(await CreateController(context).Index(
            "seguro", "active", today.AddDays(-1), today.AddDays(10), CancellationToken.None));
        var model = Assert.IsType<ReminderIndexViewModel>(result.Model);

        Assert.Single(model.Items);
        Assert.Equal(active.Id, model.Items[0].Id);
        Assert.Equal(1, model.ActiveCount);
        Assert.Equal(1, model.ScheduledCount);
        Assert.Equal(1, model.AcknowledgedCount);
        Assert.Equal(3, model.TotalCount);
        Assert.Equal("seguro", model.Search);
        Assert.Equal("active", model.Status);
    }

    /// <summary>Verifies invalid reminder forms are returned without persistence.</summary>
    [Fact]
    public async Task CreateReturnsFormWhenModelStateIsInvalid()
    {
        await using var context = CreateContext();
        var controller = CreateController(context);
        controller.ModelState.AddModelError(nameof(ReminderFormViewModel.Text), "required");
        var model = new ReminderFormViewModel { Text = string.Empty };

        var result = Assert.IsType<ViewResult>(await controller.Create(model, CancellationToken.None));

        Assert.Equal("Form", result.ViewName);
        Assert.Empty(context.Reminders);
    }

    /// <summary>Verifies creating a reminder persists the authenticated actor and confirmation feedback.</summary>
    [Fact]
    public async Task CreatePersistsReminderAndAuditActor()
    {
        await using var context = CreateContext();
        var controller = CreateController(context);
        var model = new ReminderFormViewModel
        {
            Text = "Pagar seguro",
            EventDate = new DateOnly(2026, 12, 15),
            NoticeDays = 10
        };

        var result = Assert.IsType<RedirectToActionResult>(await controller.Create(model, CancellationToken.None));
        var reminder = await context.Reminders.SingleAsync();

        Assert.Equal(nameof(RemindersController.Index), result.ActionName);
        Assert.Equal("Pagar seguro", reminder.Text);
        Assert.Equal("test-user", reminder.CreatedBy);
        Assert.Equal("Lembrete criado.", controller.TempData["SuccessMessage"]);
    }

    /// <summary>Verifies editing a reminder updates its data and clears prior acknowledgements.</summary>
    [Fact]
    public async Task EditUpdatesReminderAndReactivatesAcknowledgements()
    {
        await using var context = CreateContext();
        var reminder = new Reminder("Original", new DateOnly(2026, 10, 10), 3) { CreatedBy = "creator" };
        context.Add(reminder);
        context.ReminderAcknowledgements.Add(new ReminderAcknowledgement
        {
            ReminderId = reminder.Id,
            UserId = "another-user",
            AcknowledgedAt = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();
        var controller = CreateController(context);
        var model = new ReminderFormViewModel
        {
            Id = reminder.Id,
            Text = "Atualizado",
            EventDate = new DateOnly(2026, 11, 20),
            NoticeDays = 7
        };

        var result = Assert.IsType<RedirectToActionResult>(await controller.Edit(reminder.Id, model, CancellationToken.None));

        Assert.Equal(nameof(RemindersController.Index), result.ActionName);
        Assert.Equal("Atualizado", reminder.Text);
        Assert.Equal("test-user", reminder.UpdatedBy);
        Assert.Empty(context.ReminderAcknowledgements);
        Assert.Equal("Lembrete atualizado e reativado para todos os utilizadores.", controller.TempData["SuccessMessage"]);
    }

    /// <summary>Verifies reminder edit and acknowledgement endpoints reject missing or mismatched records.</summary>
    [Fact]
    public async Task EditAndAcknowledgeRejectInvalidIdentifiers()
    {
        await using var context = CreateContext();
        var controller = CreateController(context);
        var model = new ReminderFormViewModel { Id = Guid.NewGuid(), Text = "Teste" };

        Assert.IsType<BadRequestResult>(await controller.Edit(Guid.NewGuid(), model, CancellationToken.None));
        Assert.IsType<NotFoundResult>(await controller.Edit(model.Id, model, CancellationToken.None));
        Assert.IsType<NotFoundResult>(await controller.Acknowledge(Guid.NewGuid(), null, CancellationToken.None));
    }

    /// <summary>Verifies deleting a reminder removes it from persistence and emits success feedback.</summary>
    [Fact]
    public async Task DeleteRemovesExistingReminder()
    {
        await using var context = CreateContext();
        var reminder = new Reminder("Eliminar", new DateOnly(2026, 9, 30), 2);
        context.Add(reminder);
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = Assert.IsType<RedirectToActionResult>(await controller.Delete(reminder.Id, CancellationToken.None));

        Assert.Equal(nameof(RemindersController.Index), result.ActionName);
        Assert.Empty(context.Reminders);
        Assert.IsType<NotFoundResult>(await controller.Delete(Guid.NewGuid(), CancellationToken.None));
    }

    /// <summary>Creates an isolated in-memory application database.</summary>
    /// <returns>The test database context.</returns>
    private static DenariusDbContext CreateContext() => ControllerTestSupport.CreateContext();

    /// <summary>Creates an authenticated reminder controller with functional temporary data.</summary>
    /// <param name="context">Test database context.</param>
    /// <returns>The configured controller.</returns>
    private static RemindersController CreateController(DenariusDbContext context) =>
        ControllerTestSupport.Configure(new RemindersController(context));
}

/// <summary>Verifies warranty controller workflows against the application persistence model.</summary>
public sealed class WarrantiesControllerTests
{
    /// <summary>Verifies warranty listing trims search input and returns matching rows.</summary>
    [Fact]
    public async Task IndexFiltersByNameOrSupplier()
    {
        await using var context = CreateContext();
        context.Warranties.AddRange(
            new Warranty("Portátil", "Loja Norte", new DateOnly(2026, 1, 1), new DateOnly(2028, 1, 1), null, null, null),
            new Warranty("Televisor", "Casa Sul", new DateOnly(2026, 2, 1), new DateOnly(2029, 2, 1), null, null, null));
        await context.SaveChangesAsync();

        var result = Assert.IsType<ViewResult>(await CreateController(context).Index(" Norte ", CancellationToken.None));
        var model = Assert.IsType<WarrantyIndexViewModel>(result.Model);

        Assert.Single(model.Items);
        Assert.Equal("Portátil", model.Items[0].Name);
        Assert.Equal("Norte", model.Search);
    }

    /// <summary>Verifies warranty creation validates dates before writing warranty and reminder records.</summary>
    [Fact]
    public async Task CreateValidatesDatesAndPersistsLinkedReminder()
    {
        await using var context = CreateContext();
        var controller = CreateController(context);
        var invalid = new WarrantyFormViewModel
        {
            Name = "Equipamento",
            PurchaseDate = new DateOnly(2026, 5, 2),
            ExpiryDate = new DateOnly(2026, 5, 1),
            NoticeDays = 30
        };

        Assert.IsType<ViewResult>(await controller.Create(invalid, CancellationToken.None));
        Assert.Empty(context.Warranties);

        var valid = new WarrantyFormViewModel
        {
            Name = " Equipamento ",
            Supplier = "Fornecedor",
            PurchaseDate = new DateOnly(2026, 5, 2),
            ExpiryDate = new DateOnly(2028, 5, 2),
            NoticeDays = 45,
            Notes = "Cobertura total"
        };
        controller.ModelState.Clear();
        var result = Assert.IsType<RedirectToActionResult>(await controller.Create(valid, CancellationToken.None));
        var warranty = await context.Warranties.SingleAsync();
        var reminder = await context.Reminders.SingleAsync();

        Assert.Equal(nameof(WarrantiesController.Index), result.ActionName);
        Assert.Equal("test-user", warranty.CreatedBy);
        Assert.Equal(warranty.Id, reminder.WarrantyId);
        Assert.Equal("Fim da garantia: Equipamento", reminder.Text);
        Assert.Equal("test-user", reminder.CreatedBy);
        Assert.Equal("Garantia registada.", controller.TempData["SuccessMessage"]);
    }

    /// <summary>Verifies editing a warranty updates the linked reminder and authenticated actor.</summary>
    [Fact]
    public async Task EditUpdatesWarrantyAndLinkedReminder()
    {
        await using var context = CreateContext();
        var warranty = new Warranty("Original", "Loja", new DateOnly(2025, 1, 1), new DateOnly(2027, 1, 1), null, null, null);
        var reminder = new Reminder("Fim da garantia: Original", warranty.ExpiryDate, 30);
        reminder.LinkToWarranty(warranty.Id);
        context.AddRange(warranty, reminder);
        await context.SaveChangesAsync();
        var controller = CreateController(context);
        var model = new WarrantyFormViewModel
        {
            Id = warranty.Id,
            Name = "Atualizada",
            Supplier = "Nova Loja",
            PurchaseDate = new DateOnly(2025, 1, 1),
            ExpiryDate = new DateOnly(2028, 1, 1),
            NoticeDays = 60,
            Notes = "Nova nota"
        };

        var result = Assert.IsType<RedirectToActionResult>(await controller.Edit(warranty.Id, model, CancellationToken.None));

        Assert.Equal(nameof(WarrantiesController.Index), result.ActionName);
        Assert.Equal("Atualizada", warranty.Name);
        Assert.Equal("test-user", warranty.UpdatedBy);
        Assert.Equal("Fim da garantia: Atualizada", reminder.Text);
        Assert.Equal(60, reminder.NoticeDays);
        Assert.Equal("test-user", reminder.UpdatedBy);
    }

    /// <summary>Verifies warranty document retrieval handles missing and corrupt persisted documents safely.</summary>
    [Fact]
    public async Task DocumentRejectsMissingAndCorruptContent()
    {
        await using var context = CreateContext();
        var corrupt = new Warranty("Com documento", null, new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), null, "doc.pdf", "not-base64");
        context.Add(corrupt);
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        Assert.IsType<NotFoundResult>(await controller.Document(Guid.NewGuid(), CancellationToken.None));
        var problem = Assert.IsType<ObjectResult>(await controller.Document(corrupt.Id, CancellationToken.None));
        Assert.Equal(StatusCodes.Status500InternalServerError, problem.StatusCode);
    }

    /// <summary>Verifies deleting warranties handles both existing and missing identifiers.</summary>
    [Fact]
    public async Task DeleteRemovesExistingWarrantyAndRejectsMissingOne()
    {
        await using var context = CreateContext();
        var warranty = new Warranty("Eliminar", null, new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), null, null, null);
        context.Add(warranty);
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        Assert.IsType<RedirectToActionResult>(await controller.Delete(warranty.Id, CancellationToken.None));
        Assert.Empty(context.Warranties);
        Assert.IsType<NotFoundResult>(await controller.Delete(Guid.NewGuid(), CancellationToken.None));
    }

    /// <summary>Creates an isolated in-memory application database.</summary>
    /// <returns>The test database context.</returns>
    private static DenariusDbContext CreateContext() => ControllerTestSupport.CreateContext();

    /// <summary>Creates an authenticated warranty controller with functional temporary data.</summary>
    /// <param name="context">Test database context.</param>
    /// <returns>The configured controller.</returns>
    private static WarrantiesController CreateController(DenariusDbContext context) =>
        ControllerTestSupport.Configure(new WarrantiesController(context));
}

/// <summary>Provides shared authenticated controller setup for direct integration tests.</summary>
internal static class ControllerTestSupport
{
    /// <summary>Creates an isolated in-memory application database.</summary>
    /// <returns>The test database context.</returns>
    internal static DenariusDbContext CreateContext() => new(new DbContextOptionsBuilder<DenariusDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    /// <summary>Configures an MVC controller with an authenticated actor and in-memory temporary data.</summary>
    /// <typeparam name="TController">Controller type to configure.</typeparam>
    /// <param name="controller">Controller instance.</param>
    /// <returns>The configured controller.</returns>
    internal static TController Configure<TController>(TController controller) where TController : Controller
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "test-user")], "Test"))
        };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, new InMemoryTempDataProvider());
        return controller;
    }

    /// <summary>Stores temporary controller values in memory for one test controller.</summary>
    private sealed class InMemoryTempDataProvider : ITempDataProvider
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
