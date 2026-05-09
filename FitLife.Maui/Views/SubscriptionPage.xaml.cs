using FitLife.Maui.ViewModels;

namespace FitLife.Maui.Views;

public partial class SubscriptionPage : ContentPage
{
    public SubscriptionPage(SubscriptionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
