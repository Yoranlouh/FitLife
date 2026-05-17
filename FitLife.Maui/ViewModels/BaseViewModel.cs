using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FitLife.Maui.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [RelayCommand]
    public async Task GoToWeek()
    {
        await Shell.Current.GoToAsync("//WeekPage");
    }

    [RelayCommand]
    public async Task GoToLessons()
    {
        await Shell.Current.GoToAsync("//LessonsPage");
    }
}
