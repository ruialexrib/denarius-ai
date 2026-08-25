using DenariusAI.Application.DTOs;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DenariusAI.Web.ViewModels;

public sealed record DashboardViewModel(
    DashboardDto Dashboard,
    IReadOnlyList<SelectListItem> Years,
    IReadOnlyList<SelectListItem> Months,
    bool ShowDemonstrationDataNotice,
    IReadOnlyList<DashboardReminderViewModel> ActiveReminders,
    string WelcomeMessage,
    bool WelcomeGeneratedByAi);
