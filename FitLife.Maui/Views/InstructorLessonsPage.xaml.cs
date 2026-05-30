using FitLife.Maui.ViewModels;

namespace FitLife.Maui.Views;

public partial class InstructorLessonsPage : ContentPage
{
    public InstructorLessonsPage(InstructorLessonsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is InstructorLessonsViewModel vm)
            await vm.LoadAsync();
    }
}