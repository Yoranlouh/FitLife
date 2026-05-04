namespace SharedLibrary.Domain.Entities
{
    public class Photo
    {
        public string Id { get; set; } = null!;
        public string Url { get; set; } = null!;
        public string StorageKey { get; set; } = null!;
        public long Size { get; set; }
        public string ContentType { get; set; } = null!;
        public int EntityId { get; set; }
    }
}
