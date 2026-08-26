using DenariusAI.Infrastructure.Identity;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DenariusAI.Web.Controllers;

/// <summary>
/// Represents the UsersController type.
/// </summary>
[Authorize(Roles = ApplicationRoles.Administrator)]
public sealed class UsersController(UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var currentId = userManager.GetUserId(User); var rows = new List<UserListItemViewModel>();
        foreach (var user in userManager.Users.OrderBy(item => item.DisplayName))
        { var roles = await userManager.GetRolesAsync(user); rows.Add(new(user.Id, user.DisplayName, user.Email ?? string.Empty, roles.FirstOrDefault() ?? ApplicationRoles.User, user.Id == currentId)); }
        return View(new UserIndexViewModel(rows));
    }

    [HttpGet] public IActionResult Create() => View("Form", new UserFormViewModel());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserFormViewModel model)
    {
        ValidateRole(model); if (string.IsNullOrWhiteSpace(model.Password)) ModelState.AddModelError(nameof(model.Password), "Introduza uma palavra-passe inicial.");
        if (!ModelState.IsValid) return View("Form", model);
        var user = new ApplicationUser { DisplayName = model.DisplayName.Trim(), UserName = model.Email.Trim(), Email = model.Email.Trim(), EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, model.Password!); if (!result.Succeeded) { AddErrors(result); return View("Form", model); }
        result = await userManager.AddToRoleAsync(user, model.Role); if (!result.Succeeded) { await userManager.DeleteAsync(user); AddErrors(result); return View("Form", model); }
        TempData["SuccessMessage"] = "Utilizador criado."; return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await userManager.FindByIdAsync(id); if (user is null) return NotFound(); var roles = await userManager.GetRolesAsync(user);
        return View("Form", new UserFormViewModel { Id = user.Id, DisplayName = user.DisplayName, Email = user.Email ?? string.Empty, Role = roles.FirstOrDefault() ?? ApplicationRoles.User });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, UserFormViewModel model)
    {
        if (id != model.Id) return BadRequest(); ValidateRole(model); if (!ModelState.IsValid) return View("Form", model);
        var user = await userManager.FindByIdAsync(id); if (user is null) return NotFound(); var roles = await userManager.GetRolesAsync(user);
        if (roles.Contains(ApplicationRoles.Administrator) && model.Role != ApplicationRoles.Administrator && await AdministratorCountAsync() == 1)
        { ModelState.AddModelError(nameof(model.Role), "Tem de existir pelo menos um administrador."); return View("Form", model); }
        user.DisplayName = model.DisplayName.Trim(); user.Email = user.UserName = model.Email.Trim(); var result = await userManager.UpdateAsync(user); if (!result.Succeeded) { AddErrors(result); return View("Form", model); }
        if (!roles.Contains(model.Role)) { result = await userManager.RemoveFromRolesAsync(user, roles); if (result.Succeeded) result = await userManager.AddToRoleAsync(user, model.Role); if (!result.Succeeded) { AddErrors(result); return View("Form", model); } }
        if (!string.IsNullOrWhiteSpace(model.Password)) { var token = await userManager.GeneratePasswordResetTokenAsync(user); result = await userManager.ResetPasswordAsync(user, token, model.Password); if (!result.Succeeded) { AddErrors(result); return View("Form", model); } }
        TempData["SuccessMessage"] = "Utilizador atualizado."; return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        if (id == userManager.GetUserId(User)) { TempData["ErrorMessage"] = "Não pode eliminar o utilizador com sessão iniciada."; return RedirectToAction(nameof(Index)); }
        var user = await userManager.FindByIdAsync(id); if (user is null) return NotFound();
        if (await userManager.IsInRoleAsync(user, ApplicationRoles.Administrator) && await AdministratorCountAsync() == 1) { TempData["ErrorMessage"] = "Tem de existir pelo menos um administrador."; return RedirectToAction(nameof(Index)); }
        var result = await userManager.DeleteAsync(user); TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded ? "Utilizador eliminado." : "Não foi possível eliminar o utilizador."; return RedirectToAction(nameof(Index));
    }

    private void ValidateRole(UserFormViewModel model) { if (!ApplicationRoles.All.Contains(model.Role)) ModelState.AddModelError(nameof(model.Role), "Selecione uma permissão válida."); }
    private async Task<int> AdministratorCountAsync() => (await userManager.GetUsersInRoleAsync(ApplicationRoles.Administrator)).Count;
    private void AddErrors(IdentityResult result) { foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description); }
}
