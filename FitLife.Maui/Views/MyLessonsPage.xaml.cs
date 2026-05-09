using FitLife.Maui.ViewModels;

namespace FitLife.Maui.Views;

public partial class MyLessonsPage : ContentPage
{
    public MyLessonsPage(MyLessonsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
