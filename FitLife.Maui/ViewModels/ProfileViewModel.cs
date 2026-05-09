using CommunityToolkit.Mvvm.ComponentModel;
using SharedLibrary.Domain.Entities;

namespace FitLife.Maui.ViewModels;

public partial class ProfileViewModel : BaseViewModel
{
    [ObservableProperty]
    private string _firstName = "Jan";

    [ObservableProperty]
    private string _lastName = "Smit";

    [ObservableProperty]
    private string _profilePictureUrl = "https://ui-avatars.com/api/?name=Jan+Smit&size=128";

    [ObservableProperty]
    private string _subscriptionName = "Premium Abonnement";

    [ObservableProperty]
    private DateTime _subscriptionEndDate = DateTime.Now.AddMonths(6);

    public ProfileViewModel()
    {
        Title = "Mijn Profiel";
    }
}
