using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using TouchCursor.Support.Local.Services;

namespace TouchCursor.Main.Localization;

[MarkupExtensionReturnType(typeof(object))]
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
        // 언어 변경 시 UI가 즉시 갱신되도록 상수 문자열 대신 바인딩을 돌려준다.
        var binding = new Binding(nameof(LocalizedString.Value))
        {
            Source = LocalizationService.Instance.CreateBindable(Key, Default),
            Mode = BindingMode.OneWay
        };

        // Setter.Value 처럼 바인딩 식(BindingExpression)을 받을 수 없는 대상에는 Binding 자체를 준다.
        if (serviceProvider.GetService(typeof(IProvideValueTarget)) is not IProvideValueTarget target
            || target.TargetObject is not DependencyObject
            || target.TargetProperty is not DependencyProperty)
        {
            return binding;
        }

        return binding.ProvideValue(serviceProvider);
    }
}
