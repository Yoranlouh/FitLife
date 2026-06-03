using CommunityToolkit.Maui.Views;
using FitLife.Maui.ViewModels;

namespace FitLife.Maui.Views;

public partial class LegendPopup : Popup
{
    public List<WorkoutLegendItem> Items { get; }

    public LegendPopup(IEnumerable<WorkoutLegendItem> items)
    {
        Items = items.ToList();
        InitializeComponent();
        BindingContext = this;
    }
}