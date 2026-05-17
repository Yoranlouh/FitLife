namespace SharedLibrary.DTOs.Responses;

public class ParticipantResponse
{
    public int MemberId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsBuddy { get; set; }
}