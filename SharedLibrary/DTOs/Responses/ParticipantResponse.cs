namespace SharedLibrary.DTOs.Responses;

// DTO returned by GET /lessons/{lessonId}/participants and /waitlist.
// Shared between the API and the MAUI client via the SharedLibrary project.
public class ParticipantResponse
{
    // The participant's user ID in the database
    public int     MemberId { get; set; }

    // The participant's display name shown in the participants list
    public string  Name     { get; set; } = string.Empty;

    // Optional profile photo URL (null if the user hasn't uploaded a photo)
    public string? ImageUrl { get; set; }

    // True when this participant is a social "buddy" of the viewing user
    public bool    IsBuddy  { get; set; }
}
