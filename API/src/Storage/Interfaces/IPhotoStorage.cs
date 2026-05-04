using SharedLibrary.Domain.Entities;

namespace API.Storage.Interfaces;

public record PhotoSaveResult(string Id, string Url, string StorageKey, long Size, string ContentType, int creatorId);


public interface IPhotoStorage
{
    Task<PhotoSaveResult> SaveAsync(Stream stream,
        string contentType,
        string originalFileName,
        string folder,
        CancellationToken ct,
        int creatorId);
    Task<Photo> GetByCreatorAndType(int EntityId, string type);
}
