using FitLife.Maui.ViewModels;

namespace FitLife.Maui.Views;

public partial class InstructorParticipantListPage : ContentPage
{
    public InstructorParticipantListPage(InstructorParticipantListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    protected override bool OnBackButtonPressed()
    {
        _ = Shell.Current.GoToAsync("..");
        return true;
    }
}