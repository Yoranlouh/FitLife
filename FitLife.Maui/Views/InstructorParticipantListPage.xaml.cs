using FitLife.Maui.ViewModels;

namespace FitLife.Maui.Views;

public partial class InstructorParticipantListPage : ContentPage
{
    public InstructorParticipantListPage(InstructorParticipantListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnBackToHomeClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//HomePage");

    protected override bool OnBackButtonPressed()
    {
        _ = Shell.Current.GoToAsync("//HomePage");
        return true;
    }
}