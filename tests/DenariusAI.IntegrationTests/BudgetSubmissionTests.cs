using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using DenariusAI.Application.Services;
using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;
using DenariusAI.Infrastructure.Persistence;
using DenariusAI.Infrastructure.Persistence.Repositories;
using DenariusAI.Web.Controllers;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;

namespace DenariusAI.IntegrationTests;

/// <summary>Exercises rendered budget forms through MVC binding, controller actions and persistence.</summary>
public sealed class BudgetSubmissionTests
{
    /// <summary>Verifies that both rendered filter states and HTML decimal values survive saving in different cultures.</summary>
    /// <param name="cultureName">The culture used by the form value provider.</param>
    /// <param name="budgetedOnly">The selected budget filter state.</param>
    [Theory]
    [InlineData("pt-PT", false)]
    [InlineData("pt-PT", true)]
    [InlineData("en-US", false)]
    [InlineData("en-US", true)]
    [InlineData("de-DE", true)]
    public async Task RenderedFormSavesExactAmountsAndPreservesNavigation(string cultureName, bool budgetedOnly)
    {
        await using var app = CreateApplication();
        using var scope = app.Services.CreateScope();
        await using var context = CreateContext();
        var category = await SeedAsync(context);
        var controller = CreateController(context, scope.ServiceProvider);
        var page = Assert.IsType<ViewResult>(await controller.Index(2026, 9, category.FinancialGroupId,
            "Teste", budgetedOnly, "category", 1, 10, CancellationToken.None));
        var html = await RenderAsync(controller, page);
        var fields = ReadSaveFields(html);
        Assert.Equal(budgetedOnly.ToString().ToLowerInvariant(), fields["BudgetedOnly"].ToString());
        fields["Lines[0].Amount"] = "1234.56";

        var model = await BindAsync(controller, fields, cultureName);
        Assert.True(controller.ModelState.IsValid, DescribeErrors(controller));
        var result = Assert.IsType<RedirectToActionResult>(await controller.Save(model, CancellationToken.None));

        context.ChangeTracker.Clear();
        Assert.Equal(1234.56m, (await context.BudgetLines.SingleAsync(line => line.CategoryId == category.Id)).Amount);
        Assert.Equal(75m, (await context.BudgetLines.SingleAsync(line => line.CategoryId != category.Id)).Amount);
        Assert.Equal("Orçamento guardado com sucesso.", controller.TempData["SuccessMessage"]);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal(2026, result.RouteValues!["year"]);
        Assert.Equal(9, result.RouteValues["month"]);
        Assert.Equal(category.FinancialGroupId, result.RouteValues["groupId"]);
        Assert.Equal("Teste", result.RouteValues["search"]);
        Assert.Equal(budgetedOnly, result.RouteValues["budgetedOnly"]);
        Assert.Equal("category", result.RouteValues["sort"]);
        Assert.Equal(1, result.RouteValues["page"]);
        Assert.Equal(10, result.RouteValues["pageSize"]);
    }

    /// <summary>Verifies malformed amounts cannot overwrite existing budgets through any submit action.</summary>
    /// <param name="action">The budget submit action.</param>
    /// <param name="amount">The malformed or negative posted value, or null for a missing field.</param>
    [Theory]
    [InlineData("Save", "")]
    [InlineData("Save", "abc")]
    [InlineData("Save", "-1")]
    [InlineData("Save", "1,234")]
    [InlineData("Save", "999999999999999999999999999999999")]
    [InlineData("Save", null)]
    [InlineData("CopyLineForward", "abc")]
    [InlineData("CopyLineForward", "")]
    [InlineData("CopyLineForward", null)]
    [InlineData("CopyToNextMonth", "abc")]
    [InlineData("CopyToNextMonth", "")]
    [InlineData("CopyToNextMonth", null)]
    public async Task InvalidSubmissionDoesNotChangeAnyBudget(string action, string? amount)
    {
        await using var app = CreateApplication();
        using var scope = app.Services.CreateScope();
        await using var context = CreateContext();
        var category = await SeedAsync(context);
        var controller = CreateController(context, scope.ServiceProvider);
        var fields = CreateFields(category.Id, amount);
        var model = await BindAsync(controller, fields, "pt-PT");
        Assert.False(controller.ModelState.IsValid);

        await SubmitAsync(controller, model, category.Id, action);

        context.ChangeTracker.Clear();
        Assert.Single(await context.Budgets.ToListAsync());
        Assert.Equal(50m, (await context.BudgetLines.SingleAsync(line => line.CategoryId == category.Id)).Amount);
        Assert.Equal(75m, (await context.BudgetLines.SingleAsync(line => line.CategoryId != category.Id)).Amount);
        Assert.NotNull(controller.TempData["ErrorMessage"]);
        Assert.Null(controller.TempData["SuccessMessage"]);
    }

    /// <summary>Verifies valid copy submissions retain their decimal value and expected target periods.</summary>
    /// <param name="action">The budget copy action.</param>
    /// <param name="amount">The HTML number value to copy.</param>
    [Theory]
    [InlineData("CopyLineForward", "123.45")]
    [InlineData("CopyToNextMonth", "123.45")]
    [InlineData("CopyLineForward", "0")]
    [InlineData("CopyToNextMonth", "0")]
    public async Task CopyActionsPersistValidAmounts(string action, string amount)
    {
        await using var app = CreateApplication();
        using var scope = app.Services.CreateScope();
        await using var context = CreateContext();
        var category = await SeedAsync(context);
        var controller = CreateController(context, scope.ServiceProvider);
        var model = await BindAsync(controller, CreateFields(category.Id, amount), "pt-PT");
        Assert.True(controller.ModelState.IsValid, DescribeErrors(controller));

        await SubmitAsync(controller, model, category.Id, action);

        context.ChangeTracker.Clear();
        var lines = await context.BudgetLines.Include(line => line.Budget).Where(line => line.CategoryId == category.Id)
            .OrderBy(line => line.Budget.Month).ToListAsync();
        Assert.Equal(action == "CopyLineForward" ? new[] { 9, 10, 11, 12 } : new[] { 9, 10 },
            await context.Budgets.OrderBy(budget => budget.Month).Select(budget => budget.Month).ToArrayAsync());
        if (amount == "0") Assert.Empty(lines);
        else Assert.Equal(action == "CopyLineForward" ? 4 : 2, lines.Count);
        Assert.All(lines, line => Assert.Equal(decimal.Parse(amount, CultureInfo.InvariantCulture), line.Amount));
        Assert.NotNull(controller.TempData["SuccessMessage"]);
        Assert.Null(controller.TempData["ErrorMessage"]);
    }

    /// <summary>Builds MVC services with the application's compiled Razor views without starting its database bootstrap.</summary>
    /// <returns>A disposable application used only for MVC service resolution.</returns>
    private static WebApplication CreateApplication()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(BudgetController).Assembly.GetName().Name,
            EnvironmentName = "Development"
        });
        builder.Services.AddControllersWithViews();
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        return builder.Build();
    }

    /// <summary>Creates an isolated persistence store.</summary>
    /// <returns>The test database context.</returns>
    private static DenariusDbContext CreateContext() => new(new DbContextOptionsBuilder<DenariusDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    /// <summary>Seeds an editable line and a line excluded by the page's category search.</summary>
    /// <param name="context">The isolated database.</param>
    /// <returns>The category edited by the tests.</returns>
    private static async Task<Category> SeedAsync(DenariusDbContext context)
    {
        var group = new FinancialGroup { Name = "Despesas", Kind = FinancialGroupKind.Expense };
        var category = new Category { Name = "Teste", FinancialGroup = group };
        var other = new Category { Name = "Outra categoria", FinancialGroup = group };
        var budget = new Budget { Year = 2026, Month = 9 };
        context.AddRange(new BudgetLine { Budget = budget, Category = category, Amount = 50m },
            new BudgetLine { Budget = budget, Category = other, Amount = 75m });
        await context.SaveChangesAsync();
        return category;
    }

    /// <summary>Creates an authenticated controller using the production budget service and repository.</summary>
    /// <param name="context">The isolated database.</param>
    /// <param name="services">MVC services for binding and rendering.</param>
    /// <returns>The configured controller.</returns>
    private static BudgetController CreateController(DenariusDbContext context, IServiceProvider services)
    {
        var unitOfWork = new UnitOfWork(context, new AccountRepository(context), new JournalEntryRepository(context), new BudgetRepository(context));
        var http = new DefaultHttpContext
        {
            RequestServices = services,
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "budget-test-user")], "Test"))
        };
        var routeData = new RouteData();
        routeData.Values["controller"] = "Budget";
        routeData.Values["action"] = "Index";
        routeData.Routers.Add(new RouteCollection());
        return new BudgetController(new BudgetService(unitOfWork), context, NullLogger<BudgetController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = http, RouteData = routeData, ActionDescriptor = new ControllerActionDescriptor() },
            TempData = new TempDataDictionary(http, new MemoryTempDataProvider())
        };
    }

    /// <summary>Renders the actual budget view without the unrelated shared application layout.</summary>
    /// <param name="controller">The controller supplying view context.</param>
    /// <param name="result">The populated budget page.</param>
    /// <returns>The rendered form HTML.</returns>
    private static async Task<string> RenderAsync(BudgetController controller, ViewResult result)
    {
        var engine = controller.HttpContext.RequestServices.GetRequiredService<IRazorViewEngine>();
        var view = engine.GetView(null, "/Views/Budget/Index.cshtml", false);
        Assert.True(view.Success, string.Join(", ", view.SearchedLocations ?? []));
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        var viewContext = new ViewContext(controller.ControllerContext, view.View, result.ViewData, controller.TempData, writer, new HtmlHelperOptions());
        await view.View.RenderAsync(viewContext);
        return writer.ToString();
    }

    /// <summary>Extracts successful named input controls from the rendered save form.</summary>
    /// <param name="html">The rendered budget page.</param>
    /// <returns>The submitted input names and values.</returns>
    private static Dictionary<string, StringValues> ReadSaveFields(string html)
    {
        var form = Regex.Matches(html, "<form\\b[^>]*>(.*?)</form>", RegexOptions.Singleline)
            .Select(match => match.Groups[1].Value).Single(value => value.Contains("name=\"Lines[0].Amount\"", StringComparison.Ordinal));
        var fields = new Dictionary<string, StringValues>();
        foreach (Match input in Regex.Matches(form, "<input\\b[^>]*>"))
        {
            var name = Regex.Match(input.Value, "\\bname=\"([^\"]*)\"");
            if (!name.Success) continue;
            var value = Regex.Match(input.Value, "\\bvalue=\"([^\"]*)\"");
            fields[WebUtility.HtmlDecode(name.Groups[1].Value)] = WebUtility.HtmlDecode(value.Groups[1].Value);
        }
        return fields;
    }

    /// <summary>Builds a normal budget submission while allowing an absent amount field.</summary>
    /// <param name="categoryId">The category being edited.</param>
    /// <param name="amount">The raw amount, or null to omit it.</param>
    /// <returns>The form data.</returns>
    private static Dictionary<string, StringValues> CreateFields(Guid categoryId, string? amount)
    {
        var fields = new Dictionary<string, StringValues>
        {
            ["Year"] = "2026", ["Month"] = "9", ["BudgetedOnly"] = "false", ["Sort"] = "category",
            ["Lines[0].CategoryId"] = categoryId.ToString(), ["Lines[0].CategoryName"] = "Teste"
        };
        if (amount is not null) fields["Lines[0].Amount"] = amount;
        return fields;
    }

    /// <summary>Runs real MVC form binding and validation before invoking an action.</summary>
    /// <param name="controller">The controller receiving the form.</param>
    /// <param name="fields">Raw submitted form fields.</param>
    /// <param name="cultureName">The request culture.</param>
    /// <returns>The bound model, including invalid submissions for action safety checks.</returns>
    private static async Task<BudgetSaveViewModel> BindAsync(BudgetController controller, Dictionary<string, StringValues> fields, string cultureName)
    {
        var model = new BudgetSaveViewModel();
        await controller.TryUpdateModelAsync(model, string.Empty,
            new FormValueProvider(BindingSource.Form, new FormCollection(fields), CultureInfo.GetCultureInfo(cultureName)));
        return model;
    }

    /// <summary>Invokes the selected production action with its bound model.</summary>
    /// <param name="controller">The controller receiving the submission.</param>
    /// <param name="model">The bound form.</param>
    /// <param name="categoryId">The category to copy forward.</param>
    /// <param name="action">The selected submit action.</param>
    /// <returns>The action result.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The action name is not a budget submit action.</exception>
    private static Task<IActionResult> SubmitAsync(BudgetController controller, BudgetSaveViewModel model, Guid categoryId, string action) => action switch
    {
        "Save" => controller.Save(model, CancellationToken.None),
        "CopyLineForward" => controller.CopyLineForward(model, categoryId, CancellationToken.None),
        "CopyToNextMonth" => controller.CopyToNextMonth(model, CancellationToken.None),
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };

    /// <summary>Formats validation failures without depending on a localized message.</summary>
    /// <param name="controller">The controller carrying model state.</param>
    /// <returns>The validation keys and messages.</returns>
    private static string DescribeErrors(BudgetController controller) => string.Join("; ", controller.ModelState
        .SelectMany(entry => entry.Value!.Errors.Select(error => $"{entry.Key}: {error.ErrorMessage}")));

    /// <summary>Provides temporary action feedback without cookies or external storage.</summary>
    private sealed class MemoryTempDataProvider : ITempDataProvider
    {
        /// <summary>Returns a fresh feedback dictionary.</summary>
        /// <param name="context">The current request.</param>
        /// <returns>An empty temporary data dictionary.</returns>
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

        /// <summary>Leaves feedback in the controller-owned dictionary for assertions.</summary>
        /// <param name="context">The current request.</param>
        /// <param name="values">The feedback values.</param>
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
