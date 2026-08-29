using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DenariusAI.Web.ViewModels;

public sealed class WarrantyFormViewModel
{
    public Guid Id { get; set; }
    [Required, StringLength(200), Display(Name = "Designação")]
    public string Name { get; set; } = string.Empty;
    [StringLength(200), Display(Name = "Fornecedor ou loja")]
    public string? Supplier { get; set; }
    [Required, DataType(DataType.Date), Display(Name = "Data de compra")]
    public DateOnly PurchaseDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [Required, DataType(DataType.Date), Display(Name = "Fim da garantia")]
    public DateOnly ExpiryDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddYears(3));
    [Range(0, 3650), Display(Name = "Avisar com quantos dias de antecedência")]
    public int NoticeDays { get; set; } = 30;
    [StringLength(2000), Display(Name = "Notas")]
    public string? Notes { get; set; }
    [Display(Name = "Documento PDF")]
    public IFormFile? Document { get; set; }
    public string? ExistingDocumentFileName { get; set; }
}

public sealed record WarrantyRowViewModel(Guid Id, string Name, string? Supplier, DateOnly PurchaseDate, DateOnly ExpiryDate, string? DocumentFileName);
public sealed record WarrantyIndexViewModel(IReadOnlyList<WarrantyRowViewModel> Items, string? Search);

public sealed class CorrespondenceFormViewModel
{
    public Guid Id { get; set; }
    [Required, StringLength(250), Display(Name = "Assunto")]
    public string Subject { get; set; } = string.Empty;
    [StringLength(200), Display(Name = "Remetente")]
    public string? Sender { get; set; }
    [Required, DataType(DataType.Date), Display(Name = "Data de receção")]
    public DateOnly ReceivedDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [StringLength(2000), Display(Name = "Notas")]
    public string? Notes { get; set; }
    [Display(Name = "Documento PDF")]
    public IFormFile? Document { get; set; }
    public string? ExistingDocumentFileName { get; set; }
}

public sealed record CorrespondenceRowViewModel(Guid Id, string Subject, string? Sender, DateOnly ReceivedDate, string? DocumentFileName, int MetadataCount);
public sealed record CorrespondenceIndexViewModel(IReadOnlyList<CorrespondenceRowViewModel> Items, string? Search);

public sealed class CorrespondenceMetadataPageViewModel
{
    public Guid CorrespondenceId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public bool HasDocument { get; set; }
    public bool IsProposal { get; set; }
    public int? ExtractedCharacters { get; set; }
    public int? ExtractedPages { get; set; }
    public List<CorrespondenceMetadataRowViewModel> Items { get; set; } = [];
}

public sealed class CorrespondenceMetadataRowViewModel
{
    public Guid Id { get; set; }
    [Required, StringLength(120), Display(Name = "Chave")]
    public string Key { get; set; } = string.Empty;
    [Required, StringLength(1000), Display(Name = "Valor")]
    public string Value { get; set; } = string.Empty;
    public string? Confidence { get; set; }
    public bool Remove { get; set; }
}
