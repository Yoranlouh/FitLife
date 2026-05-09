using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharedLibrary.DTOs.Responses;
using Microsoft.Maui.Controls;

namespace FitLife.Maui.ViewModels;

public partial class LessonDetailViewModel : BaseViewModel, IQueryAttributable
{
    [ObservableProperty]
    private LessonResponse? _lesson;

    [ObservableProperty]
    private bool _isReserved;

    [ObservableProperty]
    private bool _isOnWaitlist;

    public LessonDetailViewModel()
    {
        Title = "Les Details";
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Lesson", out var lessonObj) && lessonObj is LessonResponse lesson)
        {
            Lesson = lesson;
            Title = lesson.WorkoutName;
            // Hier zou je checken of de gebruiker al gereserveerd heeft
        }
    }

    [RelayCommand]
    private async Task Reserve()
    {
        // Mock reserveren
        IsReserved = true;
        await Shell.Current.DisplayAlert("Succes", "Je bent ingeschreven voor deze les.", "OK");
    }

    [RelayCommand]
    private async Task Unregister()
    {
        // Mock afmelden
        IsReserved = false;
        await Shell.Current.DisplayAlert("Afgemeld", "Je hebt je succesvol afgemeld.", "OK");
    }

    [RelayCommand]
    private async Task JoinWaitlist()
    {
        // Mock wachtlijst
        IsOnWaitlist = true;
        await Shell.Current.DisplayAlert("Wachtlijst", "Je staat nu op de wachtlijst.", "OK");
    }
}
