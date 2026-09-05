using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DenariusAI.Web.Controllers;

/// <summary>
/// Coordinates AI assistant interactions and intelligent financial report generation.
/// </summary>
public sealed class AssistantController(IAssistantService assistantService, ILLMService llmService, ILogger<AssistantController> logger) : Controller
{
    /// <summary>
    /// Displays the assistant page with availability status and model information.
    /// </summary>
    /// <returns>The assistant view.</returns>
    [HttpGet]
    public IActionResult Index()
    {
        return View(new AssistantPageViewModel { IsAvailable = assistantService.IsAvailable, Model = llmService.Model });
    }

    /// <summary>
    /// Processes a question submitted to the AI assistant and returns the response.
    /// </summary>
    /// <param name="model">The question and conversation history.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A JSON response containing the assistant's answer and metadata.</returns>
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
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "O fornecedor de IA selecionado ainda não está configurado. Verifique as Definições da aplicação." });
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Financial assistant request failed.");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Não foi possível obter uma resposta do fornecedor de IA selecionado. Tente novamente." });
        }
    }
}
