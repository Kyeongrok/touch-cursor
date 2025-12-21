using System.Windows;
using TouchCursor.Forms.ViewModels;
using TouchCursor.Main.UI.Views;

namespace TouchCursor.Forms.UI.Views;

public class TouchCursorWindow : Window
{
    static TouchCursorWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TouchCursorWindow),
            new FrameworkPropertyMetadata(typeof(TouchCursorWindow)));
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(TouchCursorWindowViewModel),
            typeof(TouchCursorWindow), new PropertyMetadata(null, OnViewModelChanged));

    public TouchCursorWindowViewModel? ViewModel
    {
        get => (TouchCursorWindowViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private SettingsWindow? _settingsControl;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _settingsControl = GetTemplateChild("PART_SettingsControl") as SettingsWindow;

        if (_settingsControl != null && ViewModel != null)
        {
            _settingsControl.ViewModel = ViewModel.SettingsViewModel;
        }
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TouchCursorWindow control && control._settingsControl != null)
        {
            if (e.NewValue is TouchCursorWindowViewModel viewModel)
            {
                control._settingsControl.ViewModel = viewModel.SettingsViewModel;
            }
        }
    }
}
