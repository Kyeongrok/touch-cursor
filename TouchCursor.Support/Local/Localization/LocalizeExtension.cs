using System.Windows.Markup;
using TouchCursor.Support.Local.Services;

namespace TouchCursor.Support.Localization;

[MarkupExtensionReturnType(typeof(string))]
public class LocalizeExtension : MarkupExtension
{
    public string Key { get; set; } = "";
    public string? Default { get; set; }

    public LocalizeExtension() { }

    public LocalizeExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return LocalizationService.Instance.Get(Key, Default);
    }
}
