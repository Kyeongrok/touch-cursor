using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TouchCursor.Main.UI.Units;

/// <summary>
/// Custom control for managing activation key profiles
/// </summary>
[TemplatePart(Name = PART_ActivationKeyProfilesListBox, Type = typeof(ListBox))]
public class ActivationKeyProfilesControl : Control
{
    private const string PART_ActivationKeyProfilesListBox = "PART_ActivationKeyProfilesListBox";

    private ListBox? _activationKeyProfilesListBox;

    static ActivationKeyProfilesControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ActivationKeyProfilesControl),
            new FrameworkPropertyMetadata(typeof(ActivationKeyProfilesControl)));
    }

    public static readonly DependencyProperty ActivationKeyProfilesProperty =
        DependencyProperty.Register(
            nameof(ActivationKeyProfiles),
            typeof(IEnumerable),
            typeof(ActivationKeyProfilesControl),
            new PropertyMetadata(null));

    public IEnumerable? ActivationKeyProfiles
    {
        get => (IEnumerable?)GetValue(ActivationKeyProfilesProperty);
        set => SetValue(ActivationKeyProfilesProperty, value);
    }

    public static readonly DependencyProperty SelectedActivationKeyProfileProperty =
        DependencyProperty.Register(
            nameof(SelectedActivationKeyProfile),
            typeof(object),
            typeof(ActivationKeyProfilesControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public object? SelectedActivationKeyProfile
    {
        get => GetValue(SelectedActivationKeyProfileProperty);
        set => SetValue(SelectedActivationKeyProfileProperty, value);
    }

    public static readonly DependencyProperty AddActivationKeyProfileCommandProperty =
        DependencyProperty.Register(
            nameof(AddActivationKeyProfileCommand),
            typeof(ICommand),
            typeof(ActivationKeyProfilesControl),
            new PropertyMetadata(null));

    public ICommand? AddActivationKeyProfileCommand
    {
        get => (ICommand?)GetValue(AddActivationKeyProfileCommandProperty);
        set => SetValue(AddActivationKeyProfileCommandProperty, value);
    }

    public static readonly DependencyProperty RemoveActivationKeyProfileCommandProperty =
        DependencyProperty.Register(
            nameof(RemoveActivationKeyProfileCommand),
            typeof(ICommand),
            typeof(ActivationKeyProfilesControl),
            new PropertyMetadata(null));

    public ICommand? RemoveActivationKeyProfileCommand
    {
        get => (ICommand?)GetValue(RemoveActivationKeyProfileCommandProperty);
        set => SetValue(RemoveActivationKeyProfileCommandProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _activationKeyProfilesListBox = GetTemplateChild(PART_ActivationKeyProfilesListBox) as ListBox;
    }
}
