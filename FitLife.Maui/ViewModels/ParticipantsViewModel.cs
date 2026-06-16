using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FitLife.Maui.Services;
using SharedLibrary.DTOs.Responses;
using Microsoft.Maui.Controls;

namespace FitLife.Maui.ViewModels;

// ViewModel for the Participants page — displays who is enrolled in a lesson
// and who is on the waitlist.
// Implements IQueryAttributable to receive the lesson passed via Shell navigation.
public partial class ParticipantsViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IParticipantService _participantService;

    // The lesson whose participants are being viewed — set via Shell query
    [ObservableProperty]
    private LessonResponse? _lesson;

    // Summary text shown in the header, e.g. "12/20 Deelnemers"
    [ObservableProperty]
    private string _participantStats = "0/0 Deelnemers";

    // Controls the loading spinner shown while fetching from the API
    [ObservableProperty]
    private bool _isLoading;

    // Confirmed participants (active, non-cancelled reservations)
    public ObservableCollection<ParticipantItemViewModel> Participants { get; } = new();

    // Users waiting for a spot to open up
    public ObservableCollection<ParticipantItemViewModel> Waitlist { get; } = new();

    public ParticipantsViewModel(IParticipantService participantService)
    {
        _participantService = participantService;
        Title = "Deelnemers";
    }

    // Receives the lesson object from the LessonDetailPage via Shell navigation query.
    // Immediately triggers data loading so the page populates as soon as it appears.
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Lesson", out var lessonObj) && lessonObj is LessonResponse lesson)
        {
            Lesson = lesson;
            _ = LoadDataAsync();  // fire-and-forget — spinner shown via IsLoading
        }
    }

    // Fetches participants and waitlist in parallel from the API and populates
    // the observable collections. Uses IsLoading to prevent duplicate requests.
    private async Task LoadDataAsync()
    {
        if (Lesson is null || IsLoading)
            return;

        try
        {
            IsLoading = true;

            Participants.Clear();
            Waitlist.Clear();

            // Both API calls run concurrently via sequential awaits (they don't depend on each other)
            var participants = await _participantService.GetParticipantsAsync(Lesson.Id);
            var waitlist     = await _participantService.GetWaitlistAsync(Lesson.Id);

            // Map each DTO to a display-friendly view model item
            foreach (var p in participants)
                Participants.Add(new ParticipantItemViewModel
                {
                    Name          = p.Name,
                    ImageUrl      = p.ImageUrl,
                    IsBuddyVisible = p.IsBuddy
                });

            foreach (var w in waitlist)
                Waitlist.Add(new ParticipantItemViewModel
                {
                    Name          = w.Name,
                    ImageUrl      = w.ImageUrl,
                    IsBuddyVisible = w.IsBuddy
                });

            // Update the header summary with actual counts
            ParticipantStats = Translator.T("Participants_Stats", Participants.Count, Lesson.MaxParticipants);
        }
        finally
        {
            IsLoading = false;
        }
    }
}

// Lightweight view model representing a single row in the participants list.
public partial class ParticipantItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    // URL of the participant's profile photo (may be null if no photo uploaded)
    [ObservableProperty]
    private string? _imageUrl;

    // Controls visibility of the "buddy" icon for users who are friends with the viewer
    [ObservableProperty]
    private bool _isBuddyVisible;
}
