using Application.Common.Interfaces;
using LanguageExt;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.FileStorage;

public class LocalFileStorageService : IFileStorage
{
    private readonly string _storagePath;

    public LocalFileStorageService(IConfiguration configuration)
    {
        _storagePath = configuration["FileStorage:LocalPath"] ?? "wwwroot/uploads";
        
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    public async Task<Unit> UploadAsync(Stream stream, string fileFullPath, CancellationToken cancellationToken)
    {
        var fullPath = Path.Combine(_storagePath, fileFullPath);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await stream.CopyToAsync(fileStream, cancellationToken);

        return Unit.Default;
    }

    public Task<bool> DeleteAsync(string fileFullPath, CancellationToken cancellationToken)
    {
        var fullPath = Path.Combine(_storagePath, fileFullPath);
        
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}