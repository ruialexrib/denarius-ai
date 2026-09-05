using Microsoft.AspNetCore.Http;

namespace DenariusAI.Web.Models;

/// <summary>
/// Validates uploaded PDF files and converts their contents to the representation stored by document workflows.
/// </summary>
internal static class PdfUploadReader
{
    /// <summary>Defines the maximum accepted PDF upload size in bytes.</summary>
    public const long MaximumLength = 10 * 1024 * 1024;

    /// <summary>
    /// Validates and reads an uploaded PDF file.
    /// </summary>
    /// <param name="file">The uploaded file to validate and read.</param>
    /// <param name="cancellationToken">The token used to cancel the asynchronous copy operation.</param>
    /// <returns>The sanitized file name and Base64-encoded PDF contents.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the file is empty, too large, has a non-PDF extension or lacks a PDF signature.</exception>
    public static async Task<(string FileName, string Base64)> ReadAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length is <= 0 or > MaximumLength)
            throw new InvalidOperationException("O PDF deve ter um tamanho máximo de 10 MB.");
        if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Selecione um ficheiro PDF válido.");

        await using var stream = file.OpenReadStream();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();
        if (bytes.Length < 5 || bytes[0] != (byte)'%' || bytes[1] != (byte)'P' || bytes[2] != (byte)'D' || bytes[3] != (byte)'F' || bytes[4] != (byte)'-')
            throw new InvalidOperationException("O conteúdo do ficheiro não corresponde a um PDF válido.");
        return (Path.GetFileName(file.FileName), Convert.ToBase64String(bytes));
    }
}
