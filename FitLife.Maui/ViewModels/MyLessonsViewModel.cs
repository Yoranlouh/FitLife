using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FitLife.Maui.ViewModels;

public partial class MyLessonsViewModel : BaseViewModel
{
    public ObservableCollection<UserLesson> EnrolledLessons { get; } = new();

    public MyLessonsViewModel()
    {
        Title = "Mijn Lessen";
    }

    private void LoadMyLessons()
    {
        EnrolledLessons.Add(new UserLesson { Name = "Spinning", Time = DateTime.Today.AddHours(18), Instructor = "Marco", Location = "Zaal 1" });
        EnrolledLessons.Add(new UserLesson { Name = "Yoga", Time = DateTime.Today.AddDays(2).AddHours(10), Instructor = "Sarah", Location = "Zaal 3" });
    }
}

public class UserLesson
{
    public string Name { get; set; } = string.Empty;
    public DateTime Time { get; set; }
    public string Instructor { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}
