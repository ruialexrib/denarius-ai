using DenariusAI.Domain.Entities;
using DenariusAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.IntegrationTests;

public sealed class DocumentManagementTests
{
    [Fact]
    public async Task WarrantyCanBePersistedWithoutPdfAndReceiveItLater()
    {
        await using var context = CreateContext();
        var warranty = new Warranty("Máquina de lavar", "Loja", new DateOnly(2026, 1, 10), new DateOnly(2029, 1, 10), null);
        var reminder = new Reminder("Fim da garantia: Máquina de lavar", warranty.ExpiryDate, 30); reminder.LinkToWarranty(warranty.Id);
        context.AddRange(warranty, reminder); await context.SaveChangesAsync();

        var saved = await context.Warranties.SingleAsync(item => item.Id == warranty.Id);
        Assert.Null(saved.DocumentBase64);
        saved.Update(saved.Name, saved.Supplier, saved.PurchaseDate, saved.ExpiryDate, saved.Notes, "garantia.pdf", Convert.ToBase64String("%PDF-test"u8.ToArray()));
        await context.SaveChangesAsync();

        Assert.Equal("garantia.pdf", saved.DocumentFileName);
        Assert.NotNull(saved.DocumentBase64);
        var linkedReminder = await context.Reminders.SingleAsync(item => item.WarrantyId == warranty.Id);
        linkedReminder.Update("Fim da garantia: Máquina de lavar", new DateOnly(2030, 1, 10), 45);
        await context.SaveChangesAsync();
        Assert.Equal(new DateOnly(2030, 1, 10), linkedReminder.EventDate);
        Assert.Equal(45, linkedReminder.NoticeDays);
    }

    [Fact]
    public async Task CorrespondenceCanBePersistedWithoutPdf()
    {
        await using var context = CreateContext();
        var correspondence = new Correspondence("Aviso", "Entidade", new DateOnly(2026, 8, 29), "Nota");
        context.Correspondence.Add(correspondence); await context.SaveChangesAsync();

        var saved = await context.Correspondence.AsNoTracking().SingleAsync(item => item.Id == correspondence.Id);
        Assert.Equal("Aviso", saved.Subject);
        Assert.Null(saved.DocumentBase64);
    }

    [Fact]
    public async Task CorrespondenceMetadataIsPersistedAsKeyValuePairs()
    {
        await using var context = CreateContext();
        var correspondence = new Correspondence("Liquidação", "Entidade", new DateOnly(2026, 8, 29), null);
        context.Add(correspondence);
        context.CorrespondenceMetadata.AddRange(
            new CorrespondenceMetadata(correspondence.Id, "Referência", "ABC-123", "high"),
            new CorrespondenceMetadata(correspondence.Id, "Prazo", "30 dias", "low"));
        await context.SaveChangesAsync();

        var metadata = await context.CorrespondenceMetadata.AsNoTracking().Where(item => item.CorrespondenceId == correspondence.Id).ToDictionaryAsync(item => item.Key, item => item.Value);
        Assert.Equal("ABC-123", metadata["Referência"]);
        Assert.Equal("30 dias", metadata["Prazo"]);
    }

    [Fact]
    public void WarrantyRejectsAnExpiryBeforePurchase()
    {
        var action = () => new Warranty("Equipamento", null, new DateOnly(2026, 8, 29), new DateOnly(2026, 8, 28), null);
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public async Task SavingsCertificateCapitalizationUsesOneLinkedReminder()
    {
        await using var context = CreateContext();
        var certificate = new SavingsCertificate(new DateOnly(2026, 1, 1), "F-1", "Certificado familiar", 1000m, 2.5m, 1020m, new DateOnly(2026, 10, 1));
        var reminder = new Reminder("Capitalização do Certificado de Aforro F-1: Certificado familiar", certificate.NextCapitalization, 7);
        reminder.LinkToSavingsCertificate(certificate.Id); context.AddRange(certificate, reminder); await context.SaveChangesAsync();

        reminder.Update("Capitalização do Certificado de Aforro F-1: Certificado familiar", new DateOnly(2027, 1, 1), 14);
        await context.SaveChangesAsync();

        Assert.Single(await context.Reminders.Where(item => item.SavingsCertificateId == certificate.Id).ToListAsync());
        Assert.Equal(new DateOnly(2027, 1, 1), reminder.EventDate);
        Assert.Equal(14, reminder.NoticeDays);
    }

    private static DenariusDbContext CreateContext() => new(new DbContextOptionsBuilder<DenariusDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
