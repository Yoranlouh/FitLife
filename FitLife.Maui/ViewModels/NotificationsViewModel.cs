using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitLife.Maui.Services;
using System.Collections.ObjectModel;

namespace FitLife.Maui.ViewModels;

// ViewModel for the Notifications page.
// Reads from the INotificationService (which loads from the database on login)
// and exposes the list as an ObservableCollection so the UI updates automatically.
public partial class NotificationsViewModel : BaseViewModel
{
    private readonly INotificationService _svc;

    // The notification list displayed in the UI. ObservableCollection notifies
    // the UI when items are added or removed.
    public ObservableCollection<AppNotification> Items { get; } = new();

    // True when there are no notifications — used to show the empty-state UI
    [ObservableProperty]
    private bool _isEmpty = true;

    public NotificationsViewModel(INotificationService svc)
    {
        _svc  = svc;
        Title = "Notificaties";
    }

    // Marks all notifications as read, then reloads the list from the in-memory service.
    // Called every time the NotificationsPage appears so items are always current.
    public void Refresh()
    {
        _svc.MarkAllRead();  // marks read both in memory and in the database (fire-and-forget)
        Items.Clear();
        foreach (var n in _svc.Notifications)
            Items.Add(n);
        IsEmpty = Items.Count == 0;
    }

    // Command bound to the "Mark all read" button — delegates to Refresh()
    // which already calls MarkAllRead internally.
    [RelayCommand]
    private void ClearAll()
    {
        _svc.MarkAllRead();
        Refresh();
    }
}
