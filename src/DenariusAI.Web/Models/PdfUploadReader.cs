using Microsoft.AspNetCore.Http;

namespace DenariusAI.Web.Models;

internal static class PdfUploadReader
{
    public const long MaximumLength = 10 * 1024 * 1024;

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
