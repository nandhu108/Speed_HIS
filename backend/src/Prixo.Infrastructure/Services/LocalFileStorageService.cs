using Microsoft.Extensions.Configuration;
using Prixo.Application.Common.Interfaces;

namespace Prixo.Infrastructure.Services;

/// <summary>
/// Local-disk file storage rooted at FileStorage:RootPath. A production deployment
/// swaps this for blob storage behind the same IFileStorageService contract.
/// </summary>
public class LocalFileStorageService(IConfiguration configuration) : IFileStorageService
{
    private readonly string _rootPath = Path.GetFullPath(
        configuration["FileStorage:RootPath"] ?? "App_Data/uploads");

    public async Task<StoredFile> SaveAsync(
        Stream content, string subfolder, string fileName, CancellationToken ct = default)
    {
        var safeSubfolder = string.Join('/', subfolder.Split('/', '\\')
            .Select(SanitizeSegment)
            .Where(s => s.Length > 0));
        var safeFileName = $"{Guid.NewGuid():N}_{SanitizeSegment(Path.GetFileName(fileName))}";

        var directory = Path.Combine(_rootPath, safeSubfolder.Replace('/', Path.DirectorySeparatorChar));
        EnsureInsideRoot(directory);
        Directory.CreateDirectory(directory);

        var fullPath = Path.Combine(directory, safeFileName);
        EnsureInsideRoot(fullPath);
        await using (var fileStream = File.Create(fullPath))
        {
            await content.CopyToAsync(fileStream, ct);
        }

        var relativePath = $"{safeSubfolder}/{safeFileName}";
        return new StoredFile(relativePath, new FileInfo(fullPath).Length);
    }

    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken ct = default)
    {
        var fullPath = ResolvePath(relativePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("The requested file no longer exists.", relativePath);
        }
        return Task.FromResult<Stream>(File.OpenRead(fullPath));
    }

    public void Delete(string relativePath)
    {
        var fullPath = ResolvePath(relativePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    private string ResolvePath(string relativePath)
    {
        var fullPath = Path.Combine(_rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        EnsureInsideRoot(fullPath);
        return fullPath;
    }

    /// <summary>
    /// Canonicalises the path and rejects anything that resolves outside the storage root —
    /// defence-in-depth against a path-traversal segment reaching us from any caller.
    /// </summary>
    private void EnsureInsideRoot(string candidate)
    {
        var resolved = Path.GetFullPath(candidate);
        var rootWithSep = _rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? _rootPath
            : _rootPath + Path.DirectorySeparatorChar;
        if (!resolved.Equals(_rootPath, StringComparison.Ordinal)
            && !resolved.StartsWith(rootWithSep, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Resolved file path escapes the storage root.");
        }
    }

    private static string SanitizeSegment(string segment)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(segment.Where(c => !invalid.Contains(c)).ToArray()).Trim();
        // '.' and '..' survive the invalid-char filter (dots are legal in file names) but are
        // traversal tokens, not real segments — collapse them so they can never climb a level.
        if (cleaned.Length == 0 || cleaned == "." || cleaned == "..")
        {
            return "_";
        }
        return cleaned;
    }
}
