using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DenariusAI.Web.Controllers;

public sealed class AssistantController(IAssistantService assistantService, IApplicationSettingsService settingsService, ILogger<AssistantController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) => View(new AssistantPageViewModel { IsAvailable = assistantService.IsAvailable, Model = (await settingsService.GetAsync(cancellationToken)).MistralModel });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask([FromBody] AssistantQuestionViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(new { error = "Introduza uma pergunta com até 1000 caracteres." });
        try
        {
            var request = new AssistantRequestDto(model.Question, model.History.Select(item => new AssistantChatMessageDto(item.Role, item.Content)).ToList());
            var response = await assistantService.AskAsync(request, cancellationToken);
            logger.LogInformation("Financial assistant answered using {Model} and {TransactionCount} contextual transactions.", response.Model, response.TransactionCount);
            return Json(new { answer = response.Answer, model = response.Model, dataFrom = response.DataFrom.ToString("dd/MM/yyyy"), dataTo = response.DataTo.ToString("dd/MM/yyyy"), transactionCount = response.TransactionCount });
        }
        catch (InvalidOperationException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "O assistente ainda não está configurado. Adicione a chave Mistral nas Definições." });
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Financial assistant request failed.");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Não foi possível obter uma resposta da Mistral. Tente novamente." });
        }
    }
}
