namespace FitLife.Maui.Controls;

// Custom control that shows a user's profile photo or a default person icon.
// Automatically switches between the two states based on whether PhotoUrl is a real image URL.
// Placeholder services (ui-avatars.com, dicebear.com, etc.) are treated as "no photo".
public partial class UserAvatarView : ContentView
{
    // Bindable property so XAML can bind a photo URL from the ViewModel
    public static readonly BindableProperty PhotoUrlProperty =
        BindableProperty.Create(nameof(PhotoUrl), typeof(string), typeof(UserAvatarView), null,
            propertyChanged: OnPhotoUrlChanged);

    // Bindable property for the avatar diameter in device-independent pixels
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

    // Domains that serve placeholder/generated avatars rather than real user photos.
    // URLs from these domains cause the control to show the default person icon instead.
    private static readonly string[] PlaceholderDomains =
    [
        "ui-avatars.com", "dicebear.com", "gravatar.com",
        "placeholder.com", "robohash.org"
    ];

    // Returns true only when the URL points to a real photo on an allowed host.
    private static bool IsRealPhoto(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!url.StartsWith("http://",  StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return false;
        foreach (var domain in PlaceholderDomains)
            if (url.Contains(domain, StringComparison.OrdinalIgnoreCase)) return false;
        return true;
    }

    // Called by the MAUI binding system when PhotoUrl changes.
    // Shows the photo border when a real URL is detected; hides it for placeholders/null.
    private static void OnPhotoUrlChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view     = (UserAvatarView)bindable;
        var url      = newValue as string;
        var hasPhoto = IsRealPhoto(url);

        view.PhotoBorder.IsVisible = hasPhoto;
        if (hasPhoto)
            view.PhotoImage.Source = ImageSource.FromUri(new Uri(url!));
    }

    // Called when AvatarSize changes — resizes all child elements proportionally.
    // The icon inside the default border is scaled to 55% of the overall avatar size.
    private static void OnAvatarSizeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view     = (UserAvatarView)bindable;
        var size     = (double)newValue;
        var iconSize = Math.Round(size * 0.55);

        view.DefaultBorder.WidthRequest  = size;
        view.DefaultBorder.HeightRequest = size;
        view.PhotoBorder.WidthRequest    = size;
        view.PhotoBorder.HeightRequest   = size;
        view.PhotoImage.WidthRequest     = size;
        view.PhotoImage.HeightRequest    = size;

        if (view.DefaultBorder.Content is Image iconImage)
        {
            iconImage.WidthRequest  = iconSize;
            iconImage.HeightRequest = iconSize;
        }
    }
}
