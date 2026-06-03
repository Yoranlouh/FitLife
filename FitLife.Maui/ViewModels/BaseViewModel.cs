using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FitLife.Maui.ViewModels;

// Base class for all ViewModels in the MAUI app.
// Uses the MVVM (Model-View-ViewModel) pattern with CommunityToolkit.Mvvm.
// All page ViewModels inherit from this class to get shared properties and commands.
public partial class BaseViewModel : ObservableObject
{
    // The page title shown in the header — bound to the UI via {Binding Title}
    [ObservableProperty]
    private string _title = string.Empty;

    // Tracks whether an async operation is in progress.
    // Used to show loading spinners and prevent duplicate requests.
    [ObservableProperty]
    private bool _isBusy;

    // Navigates to the weekly lesson schedule overview page.
    [RelayCommand]
    public async Task GoToWeek()
    {
        await Shell.Current.GoToAsync("//WeekPage");
    }

    // Navigates to the full lesson list page.
    [RelayCommand]
    public async Task GoToLessons()
    {
        await Shell.Current.GoToAsync("//LessonsPage");
    }
}
