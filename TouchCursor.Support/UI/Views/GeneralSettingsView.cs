using System.Windows;
using System.Windows.Controls;

namespace TouchCursor.Support.UI.Views;

[TemplatePart(Name = PART_ActivationKeyProfilesListBox, Type = typeof(System.Windows.Controls.ListBox))]
public class GeneralSettingsView : System.Windows.Controls.Control
{
    private const string PART_ActivationKeyProfilesListBox = "PART_ActivationKeyProfilesListBox";

    private System.Windows.Controls.ListBox? _activationKeyProfilesListBox;

    static GeneralSettingsView()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(GeneralSettingsView),
            new FrameworkPropertyMetadata(typeof(GeneralSettingsView)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_activationKeyProfilesListBox != null)
            _activationKeyProfilesListBox.SelectionChanged -= OnActivationKeyProfileSelectionChanged;

        _activationKeyProfilesListBox = GetTemplateChild(PART_ActivationKeyProfilesListBox) as System.Windows.Controls.ListBox;

        if (_activationKeyProfilesListBox != null)
            _activationKeyProfilesListBox.SelectionChanged += OnActivationKeyProfileSelectionChanged;
    }

    private void OnActivationKeyProfileSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // SelectionChanged 이벤트를 외부로 전달 (필요시 RoutedEvent로 확장 가능)
    }
}
