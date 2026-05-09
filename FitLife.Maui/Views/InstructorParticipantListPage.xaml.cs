using FitLife.Maui.ViewModels;

namespace FitLife.Maui.Views;

public partial class InstructorParticipantListPage : ContentPage
{
    public InstructorParticipantListPage(InstructorParticipantListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
