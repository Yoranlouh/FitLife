namespace FitLife.Maui.Views;
using FitLife.Maui.ViewModels;

public partial class LessonDetailPage : ContentPage
{
	public LessonDetailPage(LessonDetailViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
