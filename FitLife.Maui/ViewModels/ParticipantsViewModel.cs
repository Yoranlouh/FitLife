using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FitLife.Maui.Services;
using SharedLibrary.DTOs.Responses;
using Microsoft.Maui.Controls;

namespace FitLife.Maui.ViewModels;

public partial class ParticipantsViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IParticipantService _participantService;

    [ObservableProperty]
    private LessonResponse? _lesson;

    [ObservableProperty]
    private string _participantStats = "0/0 Deelnemers";

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<ParticipantItemViewModel> Participants { get; } = new();
    public ObservableCollection<ParticipantItemViewModel> Waitlist { get; } = new();

    public ParticipantsViewModel(IParticipantService participantService)
    {
        _participantService = participantService;
        Title = "Deelnemers";
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Lesson", out var lessonObj) && lessonObj is LessonResponse lesson)
        {
            Lesson = lesson;
            _ = LoadDataAsync();
        }
    }

    private async Task LoadDataAsync()
    {
        if (Lesson is null || IsLoading)
        {
            return;
        }

        try
        {
            IsLoading = true;

            Participants.Clear();
            Waitlist.Clear();

            var participants = await _participantService.GetParticipantsAsync(Lesson.Id);
            var waitlist = await _participantService.GetWaitlistAsync(Lesson.Id);

            foreach (var participant in participants)
            {
                Participants.Add(new ParticipantItemViewModel
                {
                    Name = participant.Name,
                    ImageUrl = participant.ImageUrl,
                    IsBuddyVisible = participant.IsBuddy
                });
            }

            foreach (var waitlistItem in waitlist)
            {
                Waitlist.Add(new ParticipantItemViewModel
                {
                    Name = waitlistItem.Name,
                    ImageUrl = waitlistItem.ImageUrl,
                    IsBuddyVisible = waitlistItem.IsBuddy
                });
            }

            ParticipantStats = $"{Participants.Count}/{Lesson.MaxParticipants} Deelnemers";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

public partial class ParticipantItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _imageUrl;

    [ObservableProperty]
    private bool _isBuddyVisible;
}
