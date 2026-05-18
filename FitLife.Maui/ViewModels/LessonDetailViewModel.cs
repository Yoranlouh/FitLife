using FitLife.Maui.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharedLibrary.DTOs.Responses;
using Microsoft.Maui.Controls;

namespace FitLife.Maui.ViewModels;

public partial class LessonDetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IParticipantService _participantService;

    [ObservableProperty]
    private LessonResponse? _lesson;

    [ObservableProperty]
    private bool _isReserved;

    [ObservableProperty]
    private string _instructorPhone = "06-44221137";

    [ObservableProperty]
    private string _paymentMethod = "1 credit";

    [ObservableProperty]
    private int _participantCount;

    [ObservableProperty]
    private int _maxParticipants;

    [ObservableProperty]
    private bool _isOnWaitlist;

    public LessonDetailViewModel(IParticipantService participantService)
    {
        _participantService = participantService;
        Title = "Groepsevent";
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Lesson", out var lessonObj) && lessonObj is LessonResponse lesson)
        {
            Lesson = lesson;
            MaxParticipants = lesson.MaxParticipants;
            
            await LoadParticipantData();
            
            // Voor de demo/screenshot matchen als we geen echte user context hebben
            // IsReserved = true; 
        }
    }

    private async Task LoadParticipantData()
    {
        if (Lesson == null) return;

        IsBusy = true;
        try
        {
            var participants = await _participantService.GetParticipantsAsync(Lesson.Id);
            ParticipantCount = participants.Count();
            
            // Check of huidige gebruiker erbij zit (voor nu mocken of simpel checken)
            // IsReserved = participants.Any(p => p.UserId == currentUserId);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Reserve()
    {
        IsReserved = true;
        ParticipantCount++;
        await Shell.Current.DisplayAlert("Succes", "Je bent ingeschreven voor deze les.", "OK");
    }

    [RelayCommand]
    private async Task Unregister()
    {
        IsReserved = false;
        ParticipantCount--;
        await Shell.Current.DisplayAlert("Afgemeld", "Je hebt je succesvol afgemeld.", "OK");
    }

    [RelayCommand]
    private async Task JoinWaitlist()
    {
        // Mock wachtlijst
        IsOnWaitlist = true;
        await Shell.Current.DisplayAlert("Wachtlijst", "Je staat nu op de wachtlijst.", "OK");
    }

    [RelayCommand]
    private async Task GoToParticipants()
    {
        var navigationParameter = new Dictionary<string, object>
        {
            { "Lesson", Lesson! }
        };
        await Shell.Current.GoToAsync("ParticipantsPage", navigationParameter);
    }
}
