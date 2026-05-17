using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SharedLibrary.Domain.Entities;

namespace FitLife.Maui.ViewModels;

public partial class SubscriptionViewModel : BaseViewModel
{
    [ObservableProperty]
    private string _currentSubscription = "Premium Abonnement";

    [ObservableProperty]
    private DateTime _expiryDate = DateTime.Now.AddMonths(6);

    public ObservableCollection<SubscriptionOption> AvailableSubscriptions { get; } = new();

    public SubscriptionViewModel()
    {
        Title = "Abonnement Beheren";
    }

    private void LoadSubscriptions()
    {
        AvailableSubscriptions.Add(new SubscriptionOption { Name = "Basis", Price = 19.99m, Description = "Toegang tot fitness" });
        AvailableSubscriptions.Add(new SubscriptionOption { Name = "Premium", Price = 34.99m, Description = "Fitness + alle groepslessen" });
        AvailableSubscriptions.Add(new SubscriptionOption { Name = "All-in", Price = 49.99m, Description = "Fitness + Lessen + Sauna + Drinken" });
    }
}

public class SubscriptionOption
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
}
