using System.ComponentModel.DataAnnotations;
using DenariusAI.Web.Controllers;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DenariusAI.IntegrationTests;

public sealed class AuthenticationTests
{
    [Fact]
    public void DashboardRequiresAuthentication()
    {
        Assert.NotNull(typeof(HomeController).GetCustomAttributes(
            typeof(AuthorizeAttribute), inherit: true).SingleOrDefault());
    }

    [Theory]
    [InlineData(typeof(FinancialGroupsController))]
    [InlineData(typeof(CategoriesController))]
    [InlineData(typeof(AccountsController))]
    [InlineData(typeof(JournalEntriesController))]
    [InlineData(typeof(ReconciliationController))]
    [InlineData(typeof(BudgetController))]
    public void ConfigurationControllersRequireAuthentication(Type controllerType)
    {
        Assert.NotNull(controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), true).SingleOrDefault());
    }

    [Fact]
    public void LoginAllowsAnonymousUsersAndValidatesAntiforgery()
    {
        var method = typeof(AccountController).GetMethod(
            nameof(AccountController.Login), [typeof(LoginViewModel)]);

        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).SingleOrDefault());
        Assert.NotNull(method.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), true).SingleOrDefault());
    }

    [Fact]
    public void LoginRejectsInvalidEmailAndMissingPassword()
    {
        var model = new LoginViewModel { Email = "invalid" };
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            model, new ValidationContext(model), results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(LoginViewModel.Email)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(LoginViewModel.Password)));
    }

    [Fact]
    public void FinancialResetPostRequiresAntiforgery()
    {
        var method = typeof(SettingsController).GetMethods().Single(item => item.Name == nameof(SettingsController.ResetFinancialData) && item.GetParameters().FirstOrDefault()?.ParameterType == typeof(ResetFinancialDataViewModel));
        Assert.NotNull(method.GetCustomAttributes(typeof(HttpPostAttribute), true).SingleOrDefault());
        Assert.NotNull(method.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), true).SingleOrDefault());
    }

    [Fact]
    public void BulkReconciliationRequiresPostAndAntiforgery()
    {
        var method = typeof(ReconciliationController).GetMethod(nameof(ReconciliationController.ReconcileAll));

        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttributes(typeof(HttpPostAttribute), true).SingleOrDefault());
        Assert.NotNull(method.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), true).SingleOrDefault());
    }

    [Theory]
    [InlineData(2026, 8, 2026, 8, 0)]
    [InlineData(2026, 7, 2026, 8, 1)]
    [InlineData(2026, 9, 2026, 8, 1)]
    [InlineData(2025, 12, 2026, 1, 1)]
    [InlineData(2026, 6, 2026, 8, 2)]
    public void ReconciliationImportCalculatesBudgetMonthDistance(int movementYear, int movementMonth, int budgetYear, int budgetMonth, int expected)
    {
        Assert.Equal(expected, ReconciliationImportPeriodPolicy.MonthDistance(new DateOnly(movementYear, movementMonth, 1), budgetYear, budgetMonth));
    }

    [Theory]
    [InlineData(typeof(HomeController), nameof(HomeController.AcknowledgeDemonstrationData))]
    [InlineData(typeof(AccountController), nameof(AccountController.AcceptCookieConsent))]
    public void UserNoticeConfirmationsRequirePostAndAntiforgery(Type controllerType, string actionName)
    {
        var method = controllerType.GetMethods().Single(item => item.Name == actionName);

        Assert.NotNull(method.GetCustomAttributes(typeof(HttpPostAttribute), true).SingleOrDefault());
        Assert.NotNull(method.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), true).SingleOrDefault());
    }
}
