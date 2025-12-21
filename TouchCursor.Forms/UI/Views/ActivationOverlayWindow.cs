using System.Windows;
using System.Windows.Controls;

namespace TouchCursor.Forms.UI.Views;

public class ActivationOverlayWindow : Window
{
    static ActivationOverlayWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ActivationOverlayWindow),
            new FrameworkPropertyMetadata(typeof(ActivationOverlayWindow)));
    }

    public ActivationOverlayWindow()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Topmost = true;
        ShowInTaskbar = false;
        IsHitTestVisible = false;

        // Position in bottom-right corner
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - 150;
        Top = workArea.Bottom - 60;
    }

    public static readonly DependencyProperty KeyNameProperty =
        DependencyProperty.Register(nameof(KeyName), typeof(string),
            typeof(ActivationOverlayWindow), new PropertyMetadata("Space"));

    public string KeyName
    {
        get => (string)GetValue(KeyNameProperty);
        set => SetValue(KeyNameProperty, value);
    }

    public static readonly DependencyProperty IsActivatedProperty =
        DependencyProperty.Register(nameof(IsActivated), typeof(bool),
            typeof(ActivationOverlayWindow), new PropertyMetadata(false, OnIsActivatedChanged));

    public bool IsActivated
    {
        get => (bool)GetValue(IsActivatedProperty);
        set => SetValue(IsActivatedProperty, value);
    }

    private static void OnIsActivatedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ActivationOverlayWindow window)
        {
            if ((bool)e.NewValue)
            {
                window.Show();
            }
            else
            {
                window.Hide();
            }
        }
    }
}
