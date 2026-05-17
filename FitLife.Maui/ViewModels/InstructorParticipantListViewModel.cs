using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FitLife.Maui.ViewModels;

public partial class InstructorParticipantListViewModel : BaseViewModel
{
    [ObservableProperty]
    private string _lessonName = "Spinning Gevorderden";

    [ObservableProperty]
    private string _lessonTime = "Vandaag, 19:00 - 20:00";

    public ObservableCollection<Participant> Participants { get; } = new();

    public InstructorParticipantListViewModel()
    {
        Title = "Deelnemerslijst";
    }

    private void LoadParticipants()
    {
        Participants.Add(new Participant { Name = "Jan Smit", Status = "Ingecheckt" });
        Participants.Add(new Participant { Name = "Piet Janssen", Status = "Gereserveerd" });
        Participants.Add(new Participant { Name = "Marie de Vries", Status = "Ingecheckt" });
        Participants.Add(new Participant { Name = "Kees Bak", Status = "Gereserveerd" });
    }
}

public class Participant
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
