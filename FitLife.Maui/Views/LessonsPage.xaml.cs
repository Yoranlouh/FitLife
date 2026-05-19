using FitLife.Maui.ViewModels;

namespace FitLife.Maui.Views;

/// <summary>
/// Lessons list page - shows lessons filtered by selected day from the week
/// </summary>
public partial class LessonsPage : ContentPage
{
	public LessonsPage(LessonsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;

        // Debug: Log initialization
        System.Diagnostics.Debug.WriteLine("LessonsPage: Initialized");
	}

    /// <summary>
    /// Load lessons when page appears
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        System.Diagnostics.Debug.WriteLine("LessonsPage: OnAppearing called");

        if (BindingContext is LessonsViewModel vm)
        {
            System.Diagnostics.Debug.WriteLine($"LessonsPage: Loading lessons. WeekDays count: {vm.WeekDays.Count}, SelectedDate: {vm.SelectedDate:yyyy-MM-dd}");
            await vm.LoadLessonsCommand.ExecuteAsync(null);
            System.Diagnostics.Debug.WriteLine($"LessonsPage: Lessons loaded. Count: {vm.Lessons.Count}");
        }
    }
}
