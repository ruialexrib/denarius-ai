using System.Globalization;
using DenariusAI.Application.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DenariusAI.Web.Models;

public static class AccountStatementPdf
{
    private const string Ink = "#17243A";
    private const string Muted = "#607086";
    private const string Accent = "#159A70";

    public static byte[] Generate(AccountDto account, IReadOnlyList<AccountStatementLineDto> lines, DateOnly? from, DateOnly? to)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var culture = CultureInfo.GetCultureInfo("pt-PT");
        var period = from.HasValue || to.HasValue ? $"{from?.ToString("dd/MM/yyyy") ?? "—"} - {to?.ToString("dd/MM/yyyy") ?? "—"}" : "Todos os movimentos";
        return Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4); page.Margin(32);
            page.DefaultTextStyle(style => style.FontSize(8.5f).FontColor(Ink));
            page.Header().PaddingBottom(16).Row(row => { row.RelativeItem().Column(c => { c.Item().Text("DENARIUSAI").FontSize(8).Bold().FontColor(Accent); c.Item().PaddingTop(3).Text($"Extrato · {account.Name}").FontSize(17).Bold(); }); row.ConstantItem(170).AlignRight().Column(c => { c.Item().Text("PERÍODO SELECIONADO").FontSize(7).Bold().FontColor(Muted); c.Item().PaddingTop(3).Text(period).FontSize(9).SemiBold(); }); });
            page.Content().BorderTop(1).BorderColor("#DCE5E9").PaddingTop(16).Column(column =>
            {
                column.Spacing(12); column.Item().Text($"{lines.Count} movimento(s) · moeda {account.Currency}").FontColor(Muted);
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns => { columns.ConstantColumn(58); columns.RelativeColumn(2.2f); columns.RelativeColumn(1.1f); columns.ConstantColumn(63); columns.ConstantColumn(63); columns.ConstantColumn(75); });
                    table.Header(header => { Header(header, "DATA"); Header(header, "MOVIMENTO"); Header(header, "REFERÊNCIA"); Header(header, "DÉBITO", true); Header(header, "CRÉDITO", true); Header(header, "SALDO", true); });
                    foreach (var line in lines) { Cell(table, line.Date.ToString("dd/MM/yyyy")); Cell(table, line.Description, bold: true); Cell(table, line.Reference ?? "—"); Cell(table, line.Debit == 0 ? "—" : Money(line.Debit, culture), true); Cell(table, line.Credit == 0 ? "—" : Money(line.Credit, culture), true); Cell(table, Money(line.Balance, culture), true, true); }
                    if (lines.Count == 0) table.Cell().ColumnSpan(6).Padding(18).AlignCenter().Text("Não existem movimentos neste período.").FontColor(Muted);
                });
            });
            page.Footer().PaddingTop(12).Row(row => { row.RelativeItem().Text($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(7).FontColor(Muted); row.ConstantItem(80).AlignRight().DefaultTextStyle(s => s.FontSize(7).FontColor(Muted)).Text(t => { t.Span("Página "); t.CurrentPageNumber(); t.Span(" de "); t.TotalPages(); }); });
        })).GeneratePdf();
    }
    private static void Header(TableCellDescriptor cell, string text, bool right = false) { var c = cell.Cell().Background("#EDF5F2").BorderBottom(1).BorderColor("#CFE0DA").Padding(7); (right ? c.AlignRight() : c).Text(text).FontSize(7).Bold().FontColor(Muted); }
    private static void Cell(TableDescriptor table, string text, bool right = false, bool bold = false) { var c = table.Cell().BorderBottom(1).BorderColor("#E8ECEF").Padding(7); var t = (right ? c.AlignRight() : c).Text(text).FontSize(7.5f); if (bold) t.Bold(); }
    private static string Money(decimal value, CultureInfo culture) => $"{value.ToString("N2", culture)} €";
}
