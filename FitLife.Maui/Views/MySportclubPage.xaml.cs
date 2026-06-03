namespace FitLife.Maui.Views;

public partial class MySportclubPage : ContentPage
{
    private const string GymAddress = "Hogeschoollaan 1, Breda, Nederland";
    private const string GymLat     = "51.5749";
    private const string GymLng     = "4.7977";

    public MySportclubPage()
    {
        InitializeComponent();
        LoadMap();
    }

    private void LoadMap()
    {
        var html = """
            <!DOCTYPE html>
            <html>
            <head>
              <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0">
              <style>
                * { margin: 0; padding: 0; box-sizing: border-box; }
                html, body { width: 100%; height: 100%; overflow: hidden; }
                iframe { width: 100%; height: 100%; border: 0; display: block; }
              </style>
            </head>
            <body>
              <iframe
                src="https://maps.google.com/maps?q=Hogeschoollaan+1,+Breda,+Nederland&hl=nl&z=16&output=embed"
                allowfullscreen
                loading="lazy"
                referrerpolicy="no-referrer-when-downgrade">
              </iframe>
            </body>
            </html>
            """;

        MapView.Source = new HtmlWebViewSource { Html = html };
    }

    private async void OnDirectionsTapped(object sender, TappedEventArgs e)
    {
        var uri = new Uri(
            $"https://www.google.com/maps/dir/?api=1&destination={Uri.EscapeDataString(GymAddress)}");
        await Launcher.OpenAsync(uri);
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//HomePage");

    protected override bool OnBackButtonPressed()
    {
        _ = Shell.Current.GoToAsync("//HomePage");
        return true;
    }
}