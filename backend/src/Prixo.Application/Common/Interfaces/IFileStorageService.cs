namespace Prixo.Application.Common.Interfaces;

public record StoredFile(string RelativePath, long SizeBytes);

/// <summary>
/// Local-disk file storage for uploaded documents (patient photos, ID proofs, signed
/// consent scans…). Returns/consumes paths relative to the configured storage root —
/// never a raw filesystem path to a client.
/// </summary>
public interface IFileStorageService
{
    Task<StoredFile> SaveAsync(Stream content, string subfolder, string fileName, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string relativePath, CancellationToken ct = default);
    void Delete(string relativePath);
}
