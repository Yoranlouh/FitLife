using FitLife.Maui.ViewModels;

namespace FitLife.Maui.Views;

public partial class MyLessonsPage : ContentPage
{
    private readonly MyLessonsViewModel _viewModel;

    public MyLessonsPage(MyLessonsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadMyLessonsAsync();
    }
}
