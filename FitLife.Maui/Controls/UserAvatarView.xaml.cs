namespace FitLife.Maui.Controls;

public partial class UserAvatarView : ContentView
{
    public static readonly BindableProperty PhotoUrlProperty =
        BindableProperty.Create(nameof(PhotoUrl), typeof(string), typeof(UserAvatarView), null,
            propertyChanged: OnPhotoUrlChanged);

    public static readonly BindableProperty AvatarSizeProperty =
        BindableProperty.Create(nameof(AvatarSize), typeof(double), typeof(UserAvatarView), 50.0,
            propertyChanged: OnAvatarSizeChanged);

    public string? PhotoUrl
    {
        get => (string?)GetValue(PhotoUrlProperty);
        set => SetValue(PhotoUrlProperty, value);
    }

    public double AvatarSize
    {
        get => (double)GetValue(AvatarSizeProperty);
        set => SetValue(AvatarSizeProperty, value);
    }

    public UserAvatarView()
    {
        InitializeComponent();
    }

    private static readonly string[] PlaceholderDomains =
    [
        "ui-avatars.com", "dicebear.com", "gravatar.com",
        "placeholder.com", "robohash.org"
    ];

    private static bool IsRealPhoto(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return false;
        foreach (var domain in PlaceholderDomains)
            if (url.Contains(domain, StringComparison.OrdinalIgnoreCase)) return false;
        return true;
    }

    private static void OnPhotoUrlChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (UserAvatarView)bindable;
        var url = newValue as string;
        var hasPhoto = IsRealPhoto(url);

        view.PhotoBorder.IsVisible = hasPhoto;
        if (hasPhoto)
            view.PhotoImage.Source = ImageSource.FromUri(new Uri(url!));
    }

    private static void OnAvatarSizeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (UserAvatarView)bindable;
        var size = (double)newValue;
        var iconSize = Math.Round(size * 0.55);

        view.DefaultBorder.WidthRequest = size;
        view.DefaultBorder.HeightRequest = size;
        view.PhotoBorder.WidthRequest = size;
        view.PhotoBorder.HeightRequest = size;
        view.PhotoImage.WidthRequest = size;
        view.PhotoImage.HeightRequest = size;

        if (view.DefaultBorder.Content is Image iconImage)
        {
            iconImage.WidthRequest = iconSize;
            iconImage.HeightRequest = iconSize;
        }
    }
}