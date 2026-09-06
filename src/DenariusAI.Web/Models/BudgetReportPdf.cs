using System.Globalization;
using DenariusAI.Application.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DenariusAI.Web.Models;

/// <summary>
/// Generates monthly budget execution PDF reports.
/// </summary>
public static class BudgetReportPdf
{
    private const string Ink = "#17243A";
    private const string Muted = "#607086";
    private const string Accent = "#159A70";
    private const string Danger = "#B45145";

    /// <summary>
    /// Generates a monthly budget execution report.
    /// </summary>
    /// <param name="year">The budget year.</param>
    /// <param name="month">The budget month.</param>
    /// <param name="items">The budget execution items to render in their configured display order.</param>
    /// <returns>The generated PDF document bytes.</returns>
    public static byte[] Generate(int year, int month, IReadOnlyList<BudgetExecutionItemDto> items)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var culture = CultureInfo.GetCultureInfo("pt-PT");
        var period = UpperFirst(new DateTime(year, month, 1).ToString("MMMM 'de' yyyy", culture));
        var included = items.Where(item => item.Budgeted != 0m || item.Actual != 0m).ToList();
        var pages = Paginate(included, 20, 24);
        var budgeted = included.Sum(item => item.Budgeted);
        var actual = included.Sum(item => item.Actual);
        var variance = actual - budgeted;
        var execution = budgeted == 0m ? (decimal?)null : decimal.Round(actual / budgeted * 100m, 1);

        return Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(32);
            page.DefaultTextStyle(style => style.FontSize(8.5f).FontColor(Ink).LineHeight(1.25f));
            RenderPageHeader(page.Header(), period);
            page.Content().BorderTop(1).BorderColor("#DCE5E9").PaddingTop(18).Column(column =>
            {
                column.Spacing(16);
                for (var pageIndex = 0; pageIndex < pages.Count; pageIndex++)
                {
                    if (pageIndex > 0) column.Item().PageBreak();
                    if (pageIndex == 0) RenderSummary(column.Item(), budgeted, actual, variance, execution, culture);
                    column.Item().Text(pageIndex == 0 ? "Execução por categoria" : "Execução por categoria - continuação").FontSize(12).Bold();
                    RenderTable(column.Item(), pages[pageIndex], culture, included.Count == 0);
                }
            });
            RenderFooter(page.Footer());
        })).GeneratePdf();
    }

    /// <summary>Renders the report page header.</summary>
    /// <param name="container">The target header container.</param>
    /// <param name="period">The display text for the selected period.</param>
    private static void RenderPageHeader(IContainer container, string period) => container.PaddingBottom(16).Row(row =>
    {
        row.RelativeItem().Column(column =>
        {
            column.Item().Text("DENARIUSAI").FontSize(8).Bold().FontColor(Accent).LetterSpacing(.08f);
            column.Item().PaddingTop(3).Text("Relatório do orçamento mensal").FontSize(17).Bold();
        });
        row.ConstantItem(180).AlignRight().Column(column =>
        {
            column.Item().AlignRight().Text("PERÍODO SELECIONADO").FontSize(7).Bold().FontColor(Muted);
            column.Item().AlignRight().PaddingTop(3).Text(period).FontSize(9).SemiBold();
        });
    });

    /// <summary>Renders the budget summary cards.</summary>
    /// <param name="container">The target container.</param>
    /// <param name="budgeted">The total budgeted amount.</param>
    /// <param name="actual">The total realized amount.</param>
    /// <param name="variance">The difference between realized and budgeted amounts.</param>
    /// <param name="execution">The optional execution percentage.</param>
    /// <param name="culture">The culture used for monetary formatting.</param>
    private static void RenderSummary(IContainer container, decimal budgeted, decimal actual, decimal variance, decimal? execution, CultureInfo culture) => container.Row(row =>
    {
        row.Spacing(10);
        Summary(row.RelativeItem(), "ORÇAMENTADO", Money(budgeted, culture), "Limite definido para o período", "#EEF7F4", Accent);
        Summary(row.RelativeItem(), "REALIZADO", Money(actual, culture), "Movimentos associados", "#F3F6F8", Ink);
        Summary(row.RelativeItem(), "DESVIO", Money(variance, culture), variance > 0 ? "Acima do planeado" : "Dentro do planeado", variance > 0 ? "#FCEDEA" : "#EEF7F4", variance > 0 ? Danger : Accent);
        Summary(row.RelativeItem(), "EXECUÇÃO", execution.HasValue ? $"{execution:N1}%" : "-", execution > 100m ? "Orçamento ultrapassado" : "Taxa de utilização", execution > 100m ? "#FCEDEA" : "#EEF7F4", execution > 100m ? Danger : Accent);
    });

    /// <summary>Renders the execution detail table.</summary>
    /// <param name="container">The target container.</param>
    /// <param name="items">The budget execution items for the current page.</param>
    /// <param name="culture">The culture used for monetary formatting.</param>
    /// <param name="isEmpty">Whether the complete report contains no execution items.</param>
    private static void RenderTable(IContainer container, IReadOnlyList<BudgetExecutionItemDto> items, CultureInfo culture, bool isEmpty) => container.Table(table =>
    {
        table.ColumnsDefinition(columns =>
        {
            columns.RelativeColumn(1.35f); columns.RelativeColumn(1.8f);
            columns.ConstantColumn(68); columns.ConstantColumn(68); columns.ConstantColumn(68); columns.ConstantColumn(58);
        });
        table.Header(header =>
        {
            Header(header, "GRUPO"); Header(header, "CATEGORIA"); Header(header, "ORÇAMENTO", true);
            Header(header, "REALIZADO", true); Header(header, "DESVIO", true); Header(header, "EXECUÇÃO", true);
        });
        foreach (var item in items)
        {
            var variance = item.Actual - item.Budgeted;
            var execution = item.Budgeted == 0m ? (decimal?)null : decimal.Round(item.Actual / item.Budgeted * 100m, 1);
            Cell(table, item.FinancialGroupName); Cell(table, item.CategoryName, bold: true);
            Cell(table, Money(item.Budgeted, culture), right: true); Cell(table, Money(item.Actual, culture), right: true);
            Cell(table, Money(variance, culture), right: true, color: variance > 0 ? Danger : Accent);
            Cell(table, execution.HasValue ? $"{execution:N1}%" : "-", right: true, color: execution > 100m ? Danger : Ink);
        }
        if (isEmpty) table.Cell().ColumnSpan(6).Padding(18).AlignCenter().Text("Não existem categorias com orçamento ou movimentos neste período.").FontColor(Muted);
    });

    /// <summary>Renders the report footer and page numbering.</summary>
    /// <param name="container">The target footer container.</param>
    private static void RenderFooter(IContainer container) => container.PaddingTop(12).Row(row =>
    {
        row.RelativeItem().Text($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm} - valores associados ao orçamento selecionado.").FontSize(7).FontColor(Muted);
        row.ConstantItem(80).AlignRight().DefaultTextStyle(style => style.FontSize(7).FontColor(Muted)).Text(text =>
        {
            text.Span("Página "); text.CurrentPageNumber(); text.Span(" de "); text.TotalPages();
        });
    });

    /// <summary>Splits report items into page-sized groups.</summary>
    /// <param name="items">The ordered execution items.</param>
    /// <param name="firstPageSize">The maximum number of items on the first page.</param>
    /// <param name="nextPageSize">The maximum number of items on subsequent pages.</param>
    /// <returns>The paginated execution items.</returns>
    private static IReadOnlyList<IReadOnlyList<BudgetExecutionItemDto>> Paginate(IReadOnlyList<BudgetExecutionItemDto> items, int firstPageSize, int nextPageSize)
    {
        if (items.Count == 0) return [Array.Empty<BudgetExecutionItemDto>()];
        var pages = new List<IReadOnlyList<BudgetExecutionItemDto>> { items.Take(firstPageSize).ToList() };
        for (var offset = firstPageSize; offset < items.Count; offset += nextPageSize) pages.Add(items.Skip(offset).Take(nextPageSize).ToList());
        return pages;
    }

    /// <summary>Renders one summary card.</summary>
    /// <param name="container">The card container.</param>
    /// <param name="label">The summary label.</param>
    /// <param name="value">The summary value.</param>
    /// <param name="detail">The explanatory detail.</param>
    /// <param name="background">The card background color.</param>
    /// <param name="color">The value text color.</param>
    private static void Summary(IContainer container, string label, string value, string detail, string background, string color) =>
        container.Background(background).Border(1).BorderColor("#DCE5E9").CornerRadius(8).Padding(12).Column(column =>
        {
            column.Item().Text(label).FontSize(7).Bold().FontColor(Muted).LetterSpacing(.05f);
            column.Item().PaddingTop(5).Text(value).FontSize(15).Bold().FontColor(color);
            column.Item().PaddingTop(3).Text(detail).FontSize(7).FontColor(Muted);
        });

    /// <summary>Renders a table header cell.</summary>
    /// <param name="table">The table header descriptor.</param>
    /// <param name="text">The header text.</param>
    /// <param name="right">Whether the header should be right-aligned.</param>
    private static void Header(TableCellDescriptor table, string text, bool right = false)
    {
        var cell = table.Cell().Background("#EDF5F2").BorderBottom(1).BorderColor("#CFE0DA").Padding(7);
        (right ? cell.AlignRight() : cell).Text(text).FontSize(7).Bold().FontColor(Muted);
    }

    /// <summary>Renders a budget execution table cell.</summary>
    /// <param name="table">The target table.</param>
    /// <param name="text">The cell text.</param>
    /// <param name="right">Whether the cell should be right-aligned.</param>
    /// <param name="bold">Whether the cell text should be bold.</param>
    /// <param name="color">An optional text color override.</param>
    private static void Cell(TableDescriptor table, string text, bool right = false, bool bold = false, string? color = null)
    {
        var cell = table.Cell().BorderBottom(1).BorderColor("#E8ECEF").Padding(7);
        var value = (right ? cell.AlignRight() : cell).Text(text).FontSize(7.5f).FontColor(color ?? Ink);
        if (bold) value.Bold();
    }

    /// <summary>Formats a monetary amount using the supplied culture.</summary>
    /// <param name="value">The monetary value.</param>
    /// <param name="culture">The culture used to format the number.</param>
    /// <returns>The formatted amount followed by the euro symbol.</returns>
    private static string Money(decimal value, CultureInfo culture) => $"{value.ToString("N2", culture)} €";

    /// <summary>Uppercases the first character of a non-empty string.</summary>
    /// <param name="value">The text to transform.</param>
    /// <returns>The transformed string.</returns>
    private static string UpperFirst(string value) => string.IsNullOrEmpty(value) ? value : char.ToUpper(value[0]) + value[1..];
}
