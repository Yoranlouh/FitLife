using FitLife.Maui.Services;

namespace FitLife.Maui.Helpers;

// XAML markup extension for live-translated text:
//   <Label Text="{helpers:Translate Settings_Title}" />
// Produces a binding to the Translator singleton's indexer, so every bound
// label updates immediately when the language changes.
[ContentProperty(nameof(Key))]
public class TranslateExtension : IMarkupExtension<BindingBase>
{
    public string Key { get; set; } = string.Empty;

    public BindingBase ProvideValue(IServiceProvider serviceProvider) =>
        new Binding($"[{Key}]", BindingMode.OneWay, source: Translator.Instance);

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) =>
        ProvideValue(serviceProvider);
}
