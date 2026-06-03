using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FitLife.Maui.ViewModels;

// ViewModel for the Instructor's participant list view.
// Shows the instructor which members have reserved or checked into their lesson.
// Currently uses placeholder/mock data — intended to be replaced with a real API call.
public partial class InstructorParticipantListViewModel : BaseViewModel
{
    // Name of the lesson being viewed — shown in the page header
    [ObservableProperty]
    private string _lessonName = "Spinning Gevorderden";

    // Lesson time string shown in the sub-header
    [ObservableProperty]
    private string _lessonTime = "Vandaag, 19:00 - 20:00";

    // Observable list of participants — bound to the CollectionView in the page
    public ObservableCollection<Participant> Participants { get; } = new();

    public InstructorParticipantListViewModel()
    {
        Title = "Deelnemerslijst";
    }

    // Populates the list with static sample data.
    // In a production version this would call the API.
    private void LoadParticipants()
    {
        Participants.Add(new Participant { Name = "Jan Smit",       Status = "Ingecheckt" });
        Participants.Add(new Participant { Name = "Piet Janssen",   Status = "Gereserveerd" });
        Participants.Add(new Participant { Name = "Marie de Vries", Status = "Ingecheckt" });
        Participants.Add(new Participant { Name = "Kees Bak",       Status = "Gereserveerd" });
    }
}

// Simple model representing one participant row in the instructor's list.
public class Participant
{
    public string Name   { get; set; } = string.Empty;
    // Status is either "Gereserveerd" (reserved) or "Ingecheckt" (checked in)
    public string Status { get; set; } = string.Empty;
}
